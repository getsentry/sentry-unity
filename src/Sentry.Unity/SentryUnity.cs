using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Xml;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity initialization class.
    /// </summary>
    public static class SentryUnity
    {
        private static SentryUnitySDK? UnitySdk;

        /// <summary>
        /// Initializes Sentry Unity SDK while configuring the options.
        /// </summary>
        /// <param name="sentryUnityOptionsConfigure">Callback to configure the options.</param>
        public static void Init(Action<SentryUnityOptions> sentryUnityOptionsConfigure)
        {
            var options = new SentryUnityOptions();
            sentryUnityOptionsConfigure.Invoke(options);

            Init(options);
        }

        /// <summary>
        /// Initializes Sentry Unity SDK while providing an options object.
        /// </summary>
        /// <param name="options">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions options)
        {
            UnitySdk = SentryUnitySDK.Init(options);
        }

        /// <summary>
        /// Closes the Sentry Unity SDK
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Close()
        {
            UnitySdk?.Close();
            UnitySdk = null;
        }
    }
}
