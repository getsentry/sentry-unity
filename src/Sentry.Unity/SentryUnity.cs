using System;
using System.ComponentModel;
using Sentry.Extensibility;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity initialization class.
    /// </summary>
    public static class SentryUnity
    {
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
        /// <param name="sentryUnityOptions">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions sentryUnityOptions)
        {
            SentryOptionsUtility.TryAttachLogger(sentryUnityOptions);
            if (sentryUnityOptions.ShouldInitializeSdk())
            {
                sentryUnityOptions.DiagnosticLogger?.LogDebug(sentryUnityOptions.ToString());
                SentrySdk.Init(sentryUnityOptions);
            }
        }
    }
}
