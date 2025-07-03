using System;

namespace Sentry.Unity.NativeUtils;

public static class SentryPlatformServices
{
    public static ISentryUnityInfo? UnityInfo { get; set; }
    public static Action<SentryUnityOptions, ISentryUnityInfo>? PlatformConfiguration { get; set; }
}
