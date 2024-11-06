using System;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.iOS.Tests;

public class SentryNativeCocoaTests
{
    [Test]
    public void Configure_DefaultConfiguration_iOS()
    {
        var unityInfo = new TestUnityInfo { IL2CPP = false };
        var options = new SentryUnityOptions();
        SentryNativeCocoa.Configure(options, unityInfo, RuntimePlatform.IPhonePlayer);
        Assert.IsAssignableFrom<NativeScopeObserver>(options.ScopeObserver);
        Assert.IsNotNull(options.CrashedLastRun);
        Assert.True(options.EnableScopeSync);
    }

    [Test]
    public void Configure_NativeSupportDisabled_iOS()
    {
        var unityInfo = new TestUnityInfo(true, false, false) { IL2CPP = false };
        var options = new SentryUnityOptions { Native = { IosNativeSupportEnabled = false } };
        SentryNativeCocoa.Configure(options, unityInfo, RuntimePlatform.IPhonePlayer);
        Assert.Null(options.ScopeObserver);
        Assert.Null(options.CrashedLastRun);
        Assert.False(options.EnableScopeSync);
    }

    [Test]
    public void Configure_DefaultConfiguration_macOS()
    {
        var unityInfo = new TestUnityInfo { IL2CPP = false };
        var options = new SentryUnityOptions();
        // Note: can't test macOS - throws because it tries to call SentryCocoaBridgeProxy.Init()
        // but the bridge isn't loaded now...
        Assert.Throws<EntryPointNotFoundException>(() =>
            SentryNativeCocoa.Configure(options, unityInfo, RuntimePlatform.OSXPlayer));
    }

    [Test]
    public void Configure_NativeSupportDisabled_macOS()
    {
        var unityInfo = new TestUnityInfo(true, false, false) { IL2CPP = false };
        var options = new SentryUnityOptions { Native = { IosNativeSupportEnabled = false } };
        SentryNativeCocoa.Configure(options, unityInfo, RuntimePlatform.OSXPlayer);
        Assert.Null(options.ScopeObserver);
        Assert.Null(options.CrashedLastRun);
        Assert.False(options.EnableScopeSync);
    }
}
