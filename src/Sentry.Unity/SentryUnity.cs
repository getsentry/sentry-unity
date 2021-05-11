using System;
using System.ComponentModel;

namespace Sentry.Unity
{
    public static class SentryUnity
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(UnitySentryOptions unitySentryOptions)
            => SentrySdk.Init(unitySentryOptions);

        public static void Init(Action<UnitySentryOptions> unitySentryOptionsConfigure)
        {
            var unitySentryOptions = new UnitySentryOptions();
            unitySentryOptionsConfigure.Invoke(unitySentryOptions);
            Init(unitySentryOptions);
        }
    }
}
