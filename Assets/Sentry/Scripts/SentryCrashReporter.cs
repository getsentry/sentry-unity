using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

using UnityEngine;

namespace Sentry
{
    /// <summary>
    /// An interface used to process the messages sent to the crash
    /// reporting service
    /// </summary>
    public interface IErrorMessage
    {

    }

    /// <summary>
    /// A crash reporter that integrates with the Sentry crash reporting service.
    /// </summary>
    public class SentryCrashReporter
    {
        private readonly Dsn _dsn;

        private int _lastBreadcrumbPos;
        private int _noBreadcrumbs;
        private long _timeLastError;
        private Breadcrumb[] _breadcrumbs;
        private Context _sentryContext;
        private bool _initialized;
        private string _version;
        private Action<SentryExceptionMessage> _messagePreprocessingHandler;

        public Dictionary<string, string> ExtraContext
        {
            get
            {
                return _sentryContext.extra_context;
            }
        }

        public SentryCrashReporter(string dsn)
        {
            _version = Application.version;

            try
            {
                _dsn = new Dsn(dsn);
            }
            catch (Exception e)
            {
                _dsn = null;

                Debug.LogError($"Error parsing DSN: {e.Message}");
            }
        }

        public long MinTime { get; } = TimeSpan.FromMilliseconds(500).Ticks;

        public void Enable()
        {
            if (_dsn == null)
            {
                Debug.LogError("No DSN set for SentryCrashReporter");
                return;
            }

            if (!_initialized)
            {
                _breadcrumbs = new Breadcrumb[Breadcrumb.MaxBreadcrumbs];
                _initialized = true;
                _sentryContext = new Context(_version);
                Application.logMessageReceivedThreaded += OnLogMessageReceived;
            }
        }

        public void Disable()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            _initialized = false;
        }

        public void AddBreadcrumb(string message)
        {
            if (!_initialized)
            {
                Debug.LogError("Cannot AddBreadcrumb if we are not initialized");
                return;
            }

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");

            _breadcrumbs[_lastBreadcrumbPos] = new Breadcrumb(timestamp, message);

            // This is a ring buffer
            _lastBreadcrumbPos += 1;
            _lastBreadcrumbPos %= Breadcrumb.MaxBreadcrumbs;
            if (_noBreadcrumbs < Breadcrumb.MaxBreadcrumbs)
            {
                _noBreadcrumbs += 1;
            }
        }

        public Task<string> CaptureMessage(string message)
        {
            if (!_initialized)
            {
                Debug.LogError("Cannot CaptureMessage if we are not initialized");
                return null;
            }

            var completer = new TaskCompletionSource<string>();

            SendMessage(message, completer);

            return completer.Task;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
        {
            if (!_initialized)
            {
                Debug.LogError("Cannot handle log message if we are not initialized");
                return;
            }

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // only send errors, can be set somewhere what we send and what we don't
                return;
            }

            var time = DateTime.UtcNow.Ticks;

            if (time - _timeLastError <= MinTime)
            {
                return;
            }

            _timeLastError = time;

            SendException(condition, stackTrace);
        }

        private Task<string> SendException(string condition, string stackTrace)
        {
            var stack = new List<StackTraceSpec>();
            var exc = condition.Split(new char[] { ':' }, 2);
            var excType = exc[0];
            var excValue = exc[1].Substring(1); // strip the space
            var stackList = stackTrace.Split('\n');

            // The format is as follows:
            // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
            // where :lineno is optional, will be ommitted in builds
            for (var i = 0; i < stackList.Length; i++)
            {
                string functionName;
                string filename;
                int lineNo;

                var item = stackList[i];
                if (item == string.Empty)
                {
                    continue;
                }

                var closingParen = item.IndexOf(')');

                if (closingParen == -1)
                {
                    functionName = item;
                    lineNo = -1;
                    filename = string.Empty;
                }
                else
                {
                    try
                    {
                        functionName = item.Substring(0, closingParen + 1);
                        if (item.Substring(closingParen + 1, 5) != " (at ")
                        {
                            // we did something wrong, failed the check
                            Debug.Log("failed parsing " + item);
                            functionName = item;
                            lineNo = -1;
                            filename = string.Empty;
                        }
                        else
                        {
                            var colon = item.LastIndexOf(':', item.Length - 1, item.Length - closingParen);
                            if (closingParen == item.Length - 1)
                            {
                                filename = string.Empty;
                                lineNo = -1;
                            }
                            else if (colon == -1)
                            {
                                filename = item.Substring(closingParen + 6, item.Length - closingParen - 7);
                                lineNo = -1;
                            }
                            else
                            {
                                filename = item.Substring(closingParen + 6, colon - closingParen - 6);
                                lineNo = Convert.ToInt32(item.Substring(colon + 1, item.Length - 2 - colon));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        functionName = item;
                        lineNo = -1;
                        filename = string.Empty; // we have no clue
                    }
                }

                stack.Add(new StackTraceSpec(filename, functionName, lineNo));
            }

            var guid = Guid.NewGuid().ToString("N");
            var breadcrumbs = Breadcrumb.Combine(
                _breadcrumbs,
                _lastBreadcrumbPos,
                _noBreadcrumbs);

            SentryExceptionMessage messageData = new SentryExceptionMessage(
                _version,
                guid,
                excType,
                excValue,
                breadcrumbs,
                _sentryContext,
                stack);

            if (_messagePreprocessingHandler != null)
            {
                _messagePreprocessingHandler.Invoke(messageData);
            }

            var data = JsonUtility.ToJson(messageData);

            var completer = new TaskCompletionSource<string>();

            SendAsyncWebRequest(data, completer);

            return completer.Task;
        }

        private void SendMessage(string message, TaskCompletionSource<string> completer)
        {
            var guid = Guid.NewGuid().ToString("N");
            var breadcrumbs = Breadcrumb.Combine(
                _breadcrumbs,
                _lastBreadcrumbPos,
                _noBreadcrumbs);

            var data = JsonUtility.ToJson(
                new SentryMessage(_version, guid, message, breadcrumbs, _sentryContext));

            SendAsyncWebRequest(data, completer);
        }

        private void SendAsyncWebRequest(string data, TaskCompletionSource<string> completer)
        {
            var key = _dsn.publicKey;
            var secret = _dsn.secretKey;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            var authString = string.Format(
                "Sentry sentry_version=5,sentry_client=Unity0.1," +
                "sentry_timestamp={0}," +
                "sentry_key={1}," +
                "sentry_secret={2}",
                timestamp,
                key,
                secret);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_dsn.callUri);
            request.Credentials = CredentialCache.DefaultCredentials;

            request.Method = "POST";
            request.Accept = "application/json";

            // Add sentry auth string
            request.Headers.Add("X-Sentry-Auth", authString);

            // Setup body
            byte[] body = Encoding.UTF8.GetBytes(data);

            request.ContentType = "application/json";
            request.ContentLength = body.Length;

            Stream bodyStream = request.GetRequestStream();
            bodyStream.Write(body, 0, body.Length);
            bodyStream.Close();

            // Get response async
            HandleAsyncResponse(request, completer);
        }

        private void HandleAsyncResponse(WebRequest request, TaskCompletionSource<string> completer)
        {
            request.BeginGetResponse(
                result =>
                {
                    try
                    {
                        HttpWebResponse response = (HttpWebResponse)request.EndGetResponse(result);

                        if (response.StatusCode < (HttpStatusCode)200 || response.StatusCode >= (HttpStatusCode)300)
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                string body = reader.ReadToEnd();
                                string message = $"{request.Method} {request.RequestUri} failed: {body}";
                                completer.SetException(new Exception(message));
                                return;
                            }
                        }

                        MemoryStream memoryStream = new MemoryStream();

                        // We need to immediately drain to a memory stream to get around 20-30s
                        // ReadToEnd times on Android. It's still fairly mysterious.
                        using (Stream responseStream = response.GetResponseStream())
                        {
                            responseStream.CopyTo(memoryStream);
                        }

                        memoryStream.Position = 0;
                        using (StreamReader reader = new StreamReader(memoryStream))
                        {
                            string content = reader.ReadToEnd();
                            completer.SetResult(content);
                        }
                    }
                    catch (Exception e)
                    {
                        SetException(request, completer, e);
                    }
                }, null);
        }

        private void SetException(WebRequest request, TaskCompletionSource<string> completer, Exception e)
        {
            Debug.LogWarning($"HTTP request for {request.Method} {request.RequestUri.ToString()} failed: {e.Message}");

            if (e is WebException)
            {
                HttpWebResponse response = ((WebException)e).Response as HttpWebResponse;

                if (response != null)
                {
                    try
                    {
                        using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                        {
                            reader.ReadToEnd();
                        }
                    }
                    catch
                    {
                    }

                    completer.SetException(e);
                    return;
                }
            }

            completer.SetException(e);
        }

        public void SetErrorMessagePreprocessingHandler(Action<IErrorMessage> messagePreprocessingDelegate)
        {
            _messagePreprocessingHandler = messagePreprocessingDelegate;
        }
    }
}
