using System;
using System.ComponentModel;
using UnityEngine;

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
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            Init(unitySentryOptions);
        }

        /// <summary>
        /// Initializes Sentry Unity SDK while providing an options object.
        /// </summary>
        /// <param name="unitySentryOptions">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions unitySentryOptions)
        {
            unitySentryOptions.TryAttachLogger();

            // Uses the game `version` as Release unless the user defined one via the Options
            if (unitySentryOptions.Release == null)
            {
                unitySentryOptions.Release = Application.productName is string productName
                    && !string.IsNullOrWhiteSpace(productName)
                        ? $"{productName}@{Application.version}"
                        : $"{Application.version}";

                unitySentryOptions.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "Setting Sentry Release to Unity App.Version: {0}",
                    null, unitySentryOptions.Release);
            }

            unitySentryOptions.Environment = unitySentryOptions.Environment is { } environment
                ? environment
                : Application.isEditor // TODO: Should we move it out and use via IApplication something?
                    ? "editor"
                    : "production";

            SentrySdk.Init(unitySentryOptions);
        }
    }
}
