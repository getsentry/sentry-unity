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
        /// <param name="unitySentryOptionsConfigure">Callback to configure the options.</param>
        public static void Init(Action<SentryUnityOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(unitySentryOptions);

            unitySentryOptionsConfigure.Invoke(unitySentryOptions);

            SentryOptionsUtility.TryAttachLogger(unitySentryOptions);
            Init(unitySentryOptions);
        }

        /// <summary>
        /// Initializes Sentry Unity SDK while providing an options object.
        /// </summary>
        /// <param name="unitySentryOptions">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions unitySentryOptions)
        {
            unitySentryOptions.DiagnosticLogger?.LogDebug(unitySentryOptions.ToString());

            SentrySdk.Init(unitySentryOptions);
        }
    }
}
