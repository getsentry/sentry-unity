using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry
{
    [Serializable]
    public class SdkVersion
    {
        public string name = "sentry-unity";
        public string version = "0.0.1";
    }

    [Serializable]
    public class ContextPair
    {
        public string type;
        public string name;

        public ContextPair(string type, string name)
        {
            this.type = type;
            this.name = name;
        }
    }

    [Serializable]
    public class Context
    {
        public ContextPair os;
        public ContextPair os_family;
        public ContextPair device_model;
        public ContextPair device_name;
        public ContextPair device_type;
        public ContextPair gpu_name;
        public ContextPair gpu_id;
        public ContextPair gpu_type;
        public ContextPair gpu_vendor;
        public ContextPair gpu_vendor_id;
        public ContextPair app_build;
        public ContextPair app_version;

        public Context(string app_version)
        {
            os = new ContextPair("os", SystemInfo.operatingSystem);
            os_family = new ContextPair("os_family", SystemInfo.operatingSystemFamily.ToString());
            device_model = new ContextPair("device_model", SystemInfo.deviceModel);
            device_name = new ContextPair("device_name", SystemInfo.deviceName);
            device_type = new ContextPair("device_type", SystemInfo.deviceType.ToString());
            gpu_name = new ContextPair("gpu_name", SystemInfo.graphicsDeviceName);
            gpu_id = new ContextPair("gpu_id", SystemInfo.graphicsDeviceID.ToString());
            gpu_type = new ContextPair("gpu_name", SystemInfo.graphicsDeviceName);
            gpu_vendor = new ContextPair("gpu_id", SystemInfo.graphicsDeviceVendor);
            gpu_vendor_id = new ContextPair("gpu_name", SystemInfo.graphicsDeviceVendorID.ToString());
#if UNITY_EDITOR
            app_build = new ContextPair("app_build", "editor");
#else
            app_build = new _ContextPair("app_build", "build");
#endif
            this.app_version = new ContextPair("app_version", app_version);
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
        public Context contexts;
        public SdkVersion sdk = new SdkVersion();
        public List<Breadcrumb> breadcrumbs = null;

        public SentryMessage(string app_version, string event_id, string message, List<Breadcrumb> breadcrumbs)
        {
            this.event_id = event_id;
            this.message = message;
            this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            this.breadcrumbs = breadcrumbs;
            this.contexts = new Context(app_version);
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

        public SentryExceptionMessage(string app_version,
                                      string event_id,
                                      string exceptionType,
                                      string exceptionValue,
                                      List<Breadcrumb> breadcrumbs,
                                      List<StackTraceSpec> stackTrace) : base(app_version, event_id, exceptionType, breadcrumbs)
        {
            this.exception = new ExceptionContainer(new List<ExceptionSpec> { new ExceptionSpec(exceptionType, exceptionValue, stackTrace) });
        }
    }

    public class Dsn
    {
        private Uri _uri;

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
            {
                throw new ArgumentException("invalid argument - DSN cannot be empty");
            }
            _uri = new Uri(dsn);
            if (string.IsNullOrEmpty(_uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            var keys = _uri.UserInfo.Split(':');
            publicKey = keys[0];
            if (string.IsNullOrEmpty(publicKey))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            secretKey = null;
            if (keys.Length > 1)
            {
                secretKey = keys[1];
            }

            var path = _uri.AbsolutePath.Substring(0, _uri.AbsolutePath.LastIndexOf('/'));
            var projectId = _uri.AbsoluteUri.Substring(_uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrEmpty(projectId))
            {
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            }

            var builder = new UriBuilder
            {
                Scheme = _uri.Scheme,
                Host = _uri.DnsSafeHost,
                Port = _uri.Port,
                Path = string.Format("{0}/api/{1}/store/", path, projectId)
            };
            callUri = builder.Uri;
        }
    }
}