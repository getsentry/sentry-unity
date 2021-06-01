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

            SentryDefaultOptionSetter.SetRelease(unitySentryOptions);
            SentryDefaultOptionSetter.SetEnvironment(unitySentryOptions);
            SentryDefaultOptionSetter.SetCacheDirectoryPath(unitySentryOptions);

            SentrySdk.Init(unitySentryOptions);
        }
    }
}
