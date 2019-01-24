using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Sentry
{
    public class SentryRuntime
    {
        public const int MaxBreadcrumbs = 100;
        private const float MinTime = 0.5f;

        private static readonly object PadLock = new object();
        private static readonly Lazy<SentryRuntime> LazyInstance = new Lazy<SentryRuntime>(CreateInstance);

        private float _timeLastError;
        private Breadcrumb[] _breadcrumbs;
        private int _lastBreadcrumbPos;
        private int _noBreadcrumbs;
        private Dsn _dsn;
        private SentrySettings _settings;
        private volatile bool _initialized; // use volatile to allow for proper multi-threaded double checking

        public static SentryRuntime Instance => LazyInstance.Value;

        public bool Initialized => _initialized;

        public string LastErrorMessage { get; private set; } = "";

        public void Initialize()
        {
            Initialize(SentrySettings.Instance);
        }

        public void Initialize(SentrySettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }
            settings.Validate();
            
            lock (PadLock)
            {
                if (!_initialized)
                {
                    DoInitialize(settings);
                }
            }
        }

        public void Deinitialize()
        {
            lock (PadLock)
            {
                if (!_initialized)
                {
                    return;
                }

                _initialized = false;
                Application.logMessageReceivedThreaded -= HandleLogCallback;
                _dsn = null;
                _breadcrumbs = null;
            }
        }

        public AsyncOperation CaptureMessage(string message)
        {
            AssertInitialized();
            
            if (_settings.Debug)
            {
                Debug.Log("sending message to sentry...");
            }

            List<Breadcrumb> breadcrumbs = Breadcrumb.CombineBreadcrumbs(_breadcrumbs,
                _lastBreadcrumbPos,
                _noBreadcrumbs);

            SentryEvent evt = new SentryEvent(message, breadcrumbs);
            PrepareEvent(evt);

            string messageJson = JsonUtility.ToJson(evt);
            return SendMessageAsync(messageJson);
        }

        public void AddBreadcrumb(string message)
        {
            AssertInitialized();

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            _breadcrumbs[_lastBreadcrumbPos] = new Breadcrumb(timestamp, message);
            _lastBreadcrumbPos += 1;
            _lastBreadcrumbPos %= MaxBreadcrumbs;
            if (_noBreadcrumbs < MaxBreadcrumbs)
            {
                _noBreadcrumbs += 1;
            }
        }

        public void ClearLastErrorMessage()
        {
            LastErrorMessage = "";
        }

        private AsyncOperation ScheduleException(string condition, string stackTrace)
        {
            AssertInitialized();
            
            List<StackTraceSpec> stack = new List<StackTraceSpec>();
            string[] exc = condition.Split(new char[] {':'}, 2);
            string excType = exc[0];
            string excValue = exc[1].Substring(1); // strip the space

            foreach (StackTraceSpec stackTraceSpec in GetStackTraces(stackTrace))
            {
                stack.Add(stackTraceSpec);
            }

            return SendExceptionAsync(excType, excValue, stack);
        }

        private void DoInitialize(SentrySettings settings)
        {            
            _settings = settings;
            _dsn = new Dsn(_settings.Dsn);
            _breadcrumbs = new Breadcrumb[MaxBreadcrumbs];
            ClearLastErrorMessage();
            Application.logMessageReceivedThreaded += HandleLogCallback;
            _initialized = true;
        }

        private void AssertInitialized()
        {
            if (!_initialized)
            {
                lock (PadLock)
                {
                    if (!_initialized)
                    {
                        throw new SentryException("sentry not initialized");
                    }                
                }                
            }
        }

        private void HandleLogCallback(string condition, string stackTrace, LogType type)
        {
            AssertInitialized();

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // only send errors, exceptions, assertion errors
                return;
            }

            LastErrorMessage = condition;

            lock (PadLock) // send one message at a time
            {
                if (Time.time > 0 && Time.time - _timeLastError <= MinTime)
                {
                    return; // silently drop the event on the floor
                }

                _timeLastError = Time.time;
                ScheduleException(condition, stackTrace);
            }
        }

        private void PrepareEvent(SentryEvent evt)
        {
            if (_settings.Version != "") // version override
            {
                evt.release = _settings.Version;
            }

            if (_settings.SendDefaultPii)
            {
                evt.contexts.device.name = SystemInfo.deviceName;
            }

            evt.tags.deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
            evt.extra.unityVersion = Application.unityVersion;
            evt.extra.screenOrientation = Screen.orientation.ToString();
        }

        private AsyncOperation SendExceptionAsync(string exceptionType, string exceptionValue,
            List<StackTraceSpec> stackTrace)
        {
            if (_settings.Debug)
            {
                Debug.Log("sending exception to sentry...");
            }

            List<Breadcrumb> breadcrumbs = Breadcrumb.CombineBreadcrumbs(_breadcrumbs,
                _lastBreadcrumbPos,
                _noBreadcrumbs);

            SentryExceptionEvent evt = new SentryExceptionEvent(exceptionType, exceptionValue, breadcrumbs, stackTrace);
            PrepareEvent(evt);

            string messageJson = JsonUtility.ToJson(evt);

            return SendMessageAsync(messageJson);
        }

        private UnityWebRequestAsyncOperation SendMessageAsync(string message)
        {
            string sentryKey = _dsn.publicKey;
            string sentrySecret = _dsn.secretKey;

            string timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            string authString = string.Format("Sentry sentry_version=5,sentry_client=Unity0.1," +
                                              "sentry_timestamp={0}," +
                                              "sentry_key={1}," +
                                              "sentry_secret={2}",
                timestamp,
                sentryKey,
                sentrySecret);

            UnityWebRequest www = new UnityWebRequest(_dsn.callUri.ToString()) {method = "POST"};
            www.SetRequestHeader("X-Sentry-Auth", authString);
            www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(message));
            www.downloadHandler = new DownloadHandlerBuffer();
            UnityWebRequestAsyncOperation asyncOperation = www.SendWebRequest();
            asyncOperation.completed += OnSendMessageCompleted;
            return asyncOperation;
        }

        private void OnSendMessageCompleted(AsyncOperation asyncOperation)
        {
            UnityWebRequestAsyncOperation webRequestAsyncOperation = (UnityWebRequestAsyncOperation) asyncOperation;
            UnityWebRequest www = webRequestAsyncOperation.webRequest;

            if (www.isNetworkError || www.isHttpError || www.responseCode != 200)
            {
                Debug.LogWarning("error sending request to sentry: " + www.error);
            }
            else if (_settings.Debug)
            {
                Debug.Log("Sentry sent back: " + www.downloadHandler.text);
            }
        }

        private static IEnumerable<StackTraceSpec> GetStackTraces(string stackTrace)
        {
            string[] stackList = stackTrace.Split('\n');
            // the format is as follows:
            // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
            // where :lineno is optional, will be omitted in builds
            for (int i = stackList.Length - 1; i >= 0; i--)
            {
                string functionName;
                string filename;
                int lineNo;

                string item = stackList[i];
                if (item == string.Empty)
                {
                    continue;
                }

                int closingParen = item.IndexOf(')');

                if (closingParen == -1)
                {
                    continue;
                }

                try
                {
                    functionName = item.Substring(0, closingParen + 1);
                    if (item.Length < closingParen + 6)
                    {
                        // No location and no params provided. Use it as-is
                        filename = string.Empty;
                        lineNo = -1;
                    }
                    else if (item.Substring(closingParen + 1, 5) != " (at ")
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
                catch
                {
                    continue;
                }

                bool inApp;

                if (filename == string.Empty
                    // i.e: <d315a7230dee4fa58154dc9e8884174d>
                    || (filename[0] == '<' && filename[filename.Length - 1] == '>'))
                {
                    // Addresses will mess with grouping. Unless possible to symbolicate, better not to report it.
                    filename = string.Empty;
                    inApp = true; // defaults to true

                    if (functionName.Contains("UnityEngine."))
                    {
                        inApp = false;
                    }
                }
                else
                {
                    inApp = filename.Contains("Assets/");
                }

                yield return new StackTraceSpec(filename, functionName, lineNo, inApp);
            }
        }

        private static SentryRuntime CreateInstance()
        {
            return new SentryRuntime();
        }
    }
}