using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry
{
    /// <summary>
    /// Graphics device unit
    /// </summary>
    /// <remarks>
    /// The value types are not made nullable due to limitation of <see cref="JsonUtility"/>
    /// </remarks>
    /// <seealso href="https://feedback.unity3d.com/suggestions/add-support-for-nullable-types-to-jsonutility"/>
    [Serializable]
    public class Gpu
    {
        /// <summary>
        /// The name of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch:	Apple A8 GPU
        /// Samsung S7: Mali-T880
        /// </example>
        public string name;

        /// <summary>
        /// The PCI Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="vendor_id"/> uniquely identifies the GPU
        /// </remarks>
        public int id;

        /// <summary>
        /// The PCI vendor Id of the graphics device
        /// </summary>
        /// <remarks>
        /// Combined with <see cref="Id"/> uniquely identifies the GPU
        /// </remarks>
        /// <seealso href="https://docs.microsoft.com/en-us/windows-hardware/drivers/install/identifiers-for-pci-devices"/>
        /// <seealso href="http://pci-ids.ucw.cz/read/PC/"/>
        public int vendor_id;

        /// <summary>
        /// The vendor name reported by the graphic device
        /// </summary>
        /// <example>
        /// Apple, ARM, WebKit
        /// </example>
        public string vendor_name;

        /// <summary>
        /// Total GPU memory available in mega-bytes.
        /// </summary>
        public int memory_size;

        /// <summary>
        /// Device type
        /// </summary>
        /// <remarks>The low level API used</remarks>
        /// <example>Metal, Direct3D11, OpenGLES3, PlayStation4, XboxOne</example>
        public string api_type;

        /// <summary>
        /// Whether the GPU is multi-threaded rendering or not.
        /// </summary>
        /// <remarks>Type hre should be Nullable{bool} which isn't supported by JsonUtility></remarks>
        public bool multi_threaded_rendering;

        /// <summary>
        /// The Version of the API of the graphics device
        /// </summary>
        /// <example>
        /// iPod touch: Metal
        /// Android: OpenGL ES 3.2 v1.r22p0-01rel0.f294e54ceb2cb2d81039204fa4b0402e
        /// WebGL Windows: OpenGL ES 3.0 (WebGL 2.0 (OpenGL ES 3.0 Chromium))
        /// OpenGL 2.0, Direct3D 9.0c
        /// </example>
        public string version;

        /// <summary>
        /// The Non-Power-Of-Two support level
        /// </summary>
        /// <example>
        /// Full
        /// </example>
        public string npot_support;
    }
    [Serializable]
    public class SdkVersion
    {
        public string name = "sentry-unity-lite";
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
        public ContextPair app_build;
        public ContextPair app_version;

        public Gpu gpu;

        public Context(string app_version)
        {
            os = new ContextPair("os", SystemInfo.operatingSystem);
            os_family = new ContextPair("os_family", SystemInfo.operatingSystemFamily.ToString());
            device_model = new ContextPair("device_model", SystemInfo.deviceModel);
            device_name = new ContextPair("device_name", SystemInfo.deviceName);
            device_type = new ContextPair("device_type", SystemInfo.deviceType.ToString());
#if UNITY_EDITOR
            app_build = new ContextPair("app_build", "editor");
#else
            app_build = new _ContextPair("app_build", "build");
#endif
            this.app_version = new ContextPair("app_version", app_version);

            gpu = new Gpu
            {
                id = SystemInfo.graphicsDeviceID,
                name = SystemInfo.graphicsDeviceName,
                vendor_id = SystemInfo.graphicsDeviceVendorID,
                vendor_name = SystemInfo.graphicsDeviceVendor,
                memory_size = SystemInfo.graphicsMemorySize,
                multi_threaded_rendering = SystemInfo.graphicsMultiThreaded,
                npot_support = SystemInfo.npotSupport.ToString(),
                version = SystemInfo.graphicsDeviceVersion,
                api_type = SystemInfo.graphicsDeviceType.ToString()
            };
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
