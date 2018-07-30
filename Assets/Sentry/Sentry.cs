
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using UnityEngine;

namespace Sentry
{
    public class Dsn
    {
        string dsn;
        Uri uri;

        public Uri callUri;
        public string secretKey, publicKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dsn"/> class.
        /// </summary>
        /// <param name="dsn">The DSN in the format: {PROTOCOL}://{PUBLIC_KEY}@{HOST}/{PATH}{PROJECT_ID}</param>
        /// <remarks>
        /// A legacy DSN containing a secret will also be accepted: {PROTOCOL}://{PUBLIC_KEY}:{SECRET_KEY}@{HOST}/{PATH}{PROJECT_ID}
        /// </remarks>
        public Dsn(string dsn)
        {
            if (dsn == "")
                throw new ArgumentException("invalid argument - DSN cannot be empty");
            this.dsn = dsn;
            uri = new Uri(dsn);
            if (string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            var keys = uri.UserInfo.Split(':');
            publicKey = keys[0];
            if (string.IsNullOrWhiteSpace(publicKey))
                throw new ArgumentException("Invalid DSN: No public key provided.");
            secretKey = null;
            if (keys.Length > 1)
                secretKey = keys[1];
            
            var path = uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/'));
            var projectId = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrWhiteSpace(projectId))
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            
            var builder = new UriBuilder
            {
                Scheme = uri.Scheme,
                Host = uri.DnsSafeHost,
                Port = uri.Port,
                Path = $"{path}/api/{projectId}/store/"
            };
            callUri = builder.Uri;

        }
    }    

    public class SentrySdk
    {
        Dsn _dsn;
        HttpClient client;

        public SentrySdk(string dsn)
        {
            _dsn = new Dsn(dsn);
            client = new HttpClient();
            var sentryKey = _dsn.publicKey;
            var sentrySecret = _dsn.secretKey;

            var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Sentry-Auth",
                     $"Sentry sentry_version=5,sentry_client=Unity0.1," +
                     $"sentry_timestamp={timestamp}," +
                     $"sentry_key={sentryKey},sentry_secret={sentrySecret}");
        }


        public void HandleLogCallback(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                // only send errors, can be set somewhere what we send and what we don't
                return;
            var stack = new List<StackTraceSpec>();
            var exc = condition.Split(new char[] { ':' }, 2);
            var excType = exc[0];
            var excValue = exc[1].Substring(1); // strip the space
            var stackList = stackTrace.Split('\n');
            // the format is as follows:
            // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
            // where :lineno is optional, will be ommitted in builds
            for (var i = 0; i < stackList.Length; i++)
            {
                string functionName, filename;
                int lineNo;

                var item = stackList[i];
                if (item == "")
                    continue;
                var firstSpace = item.IndexOf(' ');

                if (firstSpace == -1)
                {
                    functionName = item;
                    lineNo = -1;
                    filename = "";
                }
                else
                {
                    functionName = item.Substring(0, firstSpace);
                    // we can try to split functionName into module.function, but it's not 100% clear how
                    var closingParen = item.IndexOf(')', firstSpace);
                    if (closingParen == item.Length - 1)
                    {
                        // case of some continuations where there is no space between
                        // the () and the method name
                        closingParen = firstSpace - 1;
                    }
                    var colon = item.IndexOf(':', closingParen);
                    if (colon == -1)
                    {
                        Debug.Log(item);
                        filename = item.Substring(closingParen + 6, item.Length - closingParen - 7);
                        lineNo = -1;
                    }
                    else
                    {
                        filename = item.Substring(closingParen + 6, colon - closingParen - 6);
                        lineNo = Convert.ToInt32(item.Substring(colon + 1, item.Length - 2 - colon));
                    }
                }
                stack.Add(new StackTraceSpec(filename, functionName, lineNo));
            }
            sendException(excType, excValue, stack);
        }

        private long ConvertToTimestamp(DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public class _SentrySdk
        {
            public string name = "sentry-unity";
            public string version = "0.0.1";
        }

        public class SentryMessage
        {
            public string event_id;
            public string message;
            public string timestamp;
            public string logger = "error";
            public string platform = "csharp";
            public _SentrySdk sdkSpec = new _SentrySdk();

            public SentryMessage(string event_id, string message)
            {
                this.event_id = event_id;
                this.message = message;
                this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            }
        }

        [Serializable]
        public class StackTraceContainer
        {
            public List<StackTraceSpec> frames;

            public StackTraceContainer(List<StackTraceSpec> frames)
            {
                this.frames = frames;
            }
        }

        [Serializable]
        public class StackTraceSpec
        {
            public string filename;
            public string function;
            public string module = "";
            public int lineno;

            public StackTraceSpec(string filename, string function, int lineNo)
            {
                this.filename = filename;
                this.function = function;
                lineno = lineNo;
            }
        }

        [Serializable]
        public class ExceptionSpec
        {
            public string type;
            public string value;
            public StackTraceContainer stacktrace;

            public ExceptionSpec(string type, string value, List<StackTraceSpec> stacktrace)
            {
                this.type = type;
                this.value = value;
                this.stacktrace = new StackTraceContainer(stacktrace);
            }
        }

        [Serializable]
        public class ExceptionContainer
        {
            public List<ExceptionSpec> values;

            public ExceptionContainer(List<ExceptionSpec> arg)
            {
                values = arg;
            }
        }

        public class SentryExceptionMessage : SentryMessage
        {
            public ExceptionContainer exception;

            public SentryExceptionMessage(string event_id,
                                          string exceptionType,
                                          string exceptionValue,
                                          List<StackTraceSpec> stackTrace) : base(event_id, exceptionType)
            {
                this.exception = new ExceptionContainer(new List<ExceptionSpec> { new ExceptionSpec(exceptionType, exceptionValue, stackTrace) });
            }
        }

        public async void sendMessage(string msg)
        {
            var guid = Guid.NewGuid().ToString("N");
            var content = new StringContent(JsonUtility.ToJson(new SentryMessage(
                guid, msg)));

            var response = await client.PostAsync(_dsn.callUri, content);

            var responseString = await response.Content.ReadAsStringAsync();
            Debug.Log(responseString);
        }

        public async void sendException(string exceptionType, string exceptionValue, List<StackTraceSpec> stackTrace)
        {
            var guid = Guid.NewGuid().ToString("N");
            var s = JsonUtility.ToJson(
                new SentryExceptionMessage(guid, exceptionType, exceptionValue, stackTrace));
            Debug.Log(s);
            var content = new StringContent(s);

            var response = await client.PostAsync(_dsn.callUri, content);

            var responseString = await response.Content.ReadAsStringAsync();
            Debug.Log(responseString);
        }
    }
}