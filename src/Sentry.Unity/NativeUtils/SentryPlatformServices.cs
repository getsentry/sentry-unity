using System;

namespace Sentry.Unity.NativeUtils;

/// <summary>
/// These are SDK's services that are only available at runtime and cannot be baked into the SDK. The
/// <c>SentryInitialization.cs</c> is provided as <c>.cs</c> and gets compiled with the game. It sets <c>IUnityInfo</c>
/// and the <c>PlatformConfiguration</c> callback during the game's startup so that they are available during initializtion.
/// </summary>
/// <remarks>Consider this <c>internal</c>.</remarks>
public static class SentryPlatformServices
{
    public static ISentryUnityInfo? UnityInfo { get; set; }
    public static Action<SentryUnityOptions, ISentryUnityInfo>? PlatformConfiguration { get; set; }
}
