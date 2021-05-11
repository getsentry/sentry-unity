using System;
using System.ComponentModel;

namespace Sentry.Unity
{
    public static class SentryUnity
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions sentryUnityOptions)
            => SentrySdk.Init(sentryUnityOptions);

        public static void Init(Action<SentryUnityOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new SentryUnityOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            Init(unitySentryOptions);
        }
    }
}
