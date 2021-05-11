using System;
using System.ComponentModel;

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
        public static void Init(Action<UnitySentryOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            Init(unitySentryOptions);
        }

        /// <summary>
        /// Initializes Sentry Unity SDK while providing an options object.
        /// </summary>
        /// <param name="unitySentryOptions">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(UnitySentryOptions unitySentryOptions)
            => SentrySdk.Init(unitySentryOptions);
    }
}
