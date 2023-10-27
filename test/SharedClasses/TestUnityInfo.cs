using Sentry.Unity;
using UnityEngine;

public class TestUnityInfo : ISentryUnityInfo
{
    private readonly bool _isKnownPlatform;
    private readonly bool _isLinux;
    private readonly bool _isNativeSupportEnabled;

    public bool IL2CPP { get; set; }
    public string? Platform { get; }
    public Il2CppMethods? Il2CppMethods { get; }

    public TestUnityInfo(bool isKnownPlatform = true, bool isLinux = false, bool isNativeSupportEnabled = true)
    {
        _isKnownPlatform = isKnownPlatform;
        _isLinux = isLinux;
        _isNativeSupportEnabled = isNativeSupportEnabled;
    }

    public bool IsKnownPlatform() => _isKnownPlatform;
    public bool IsLinux() => _isLinux;

    public bool IsNativeSupportEnabled(SentryUnityOptions options, RuntimePlatform platform) => _isNativeSupportEnabled;
}
