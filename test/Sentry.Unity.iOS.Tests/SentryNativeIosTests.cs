using System;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.iOS.Tests
{
    public class TestSentryUnityInfo : ISentryUnityInfo
    {
        public bool IL2CPP { get; set; }
        public string? Platform { get; }
        public Il2CppMethods? Il2CppMethods { get; }
        public bool IsKnownPlatform() => true;
    }

    public class SentryNativeCocoaTests
    {
        private TestSentryUnityInfo _sentryUnityInfo = null!;

        [SetUp]
        public void SetUp()
        {
            _sentryUnityInfo = new TestSentryUnityInfo { IL2CPP = false };
        }

        [Test]
        public void Configure_DefaultConfiguration_iOS()
        {
            var options = new SentryUnityOptions();
            SentryNativeCocoa.Configure(options, _sentryUnityInfo, RuntimePlatform.IPhonePlayer);
            Assert.IsAssignableFrom<NativeScopeObserver>(options.ScopeObserver);
            Assert.IsNotNull(options.CrashedLastRun);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeSupportDisabled_iOS()
        {
            var options = new SentryUnityOptions { IosNativeSupportEnabled = false };
            SentryNativeCocoa.Configure(options, _sentryUnityInfo, RuntimePlatform.IPhonePlayer);
            Assert.Null(options.ScopeObserver);
            Assert.Null(options.CrashedLastRun);
            Assert.False(options.EnableScopeSync);
        }

        [Test]
        public void Configure_DefaultConfiguration_macOS()
        {
            var options = new SentryUnityOptions();
            // Note: can't test macOS - throws because it tries to call SentryCocoaBridgeProxy.Init()
            // but the bridge isn't loaded now...
            Assert.Throws<EntryPointNotFoundException>(() =>
                SentryNativeCocoa.Configure(options, _sentryUnityInfo, RuntimePlatform.OSXPlayer));
        }

        [Test]
        public void Configure_NativeSupportDisabled_macOS()
        {
            var options = new SentryUnityOptions { MacosNativeSupportEnabled = false };
            SentryNativeCocoa.Configure(options, _sentryUnityInfo, RuntimePlatform.OSXPlayer);
            Assert.Null(options.ScopeObserver);
            Assert.Null(options.CrashedLastRun);
            Assert.False(options.EnableScopeSync);
        }
    }
}
