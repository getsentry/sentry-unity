using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry
{
    [Serializable]
    public class _SentrySdk
    {
        public string name = "sentry-unity";
        public string version = "0.0.1";
    }

    [Serializable]
    public class _ContextPair
    {
        public string type;
        public string name;

        public _ContextPair(string type, string name)
        {
            this.type = type;
            this.name = name;
        }
    }

    [Serializable]
    public class _Context
    {
        public _ContextPair os;

        public _Context()
        {
            os = new _ContextPair("os", SystemInfo.operatingSystem);
        }
    }

    [Serializable]
    public class SentryMessage
    {
        public string event_id;
        public string message;
        public string timestamp;
        public string logger = "error";
        public string platform = "csharp";
        public _Context contexts = new _Context();
        public _SentrySdk sdk = new _SentrySdk();
        public List<Breadcrumb> breadcrumbs = null;

        public SentryMessage(string event_id, string message, List<Breadcrumb> breadcrumbs)
        {
            this.event_id = event_id;
            this.message = message;
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            this.breadcrumbs = breadcrumbs;
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
                                      List<Breadcrumb> breadcrumbs,
                                      List<StackTraceSpec> stackTrace) : base(event_id, exceptionType, breadcrumbs)
        {
            this.exception = new ExceptionContainer(new List<ExceptionSpec> { new ExceptionSpec(exceptionType, exceptionValue, stackTrace) });
        }
    }

    public class Dsn
    {
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
}