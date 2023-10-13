using System;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity;

namespace Sentry.Unity.Android.Tests
{
    public class SentryNativeAndroidTests
    {
        private bool _reinstallCalled;
        private Action? _originalReinstallSentryNativeBackendStrategy;
        private Action _fakeReinstallSentryNativeBackendStrategy;
        private TestSentryUnityInfo _sentryUnityInfo = null!;

        public SentryNativeAndroidTests()
            => _fakeReinstallSentryNativeBackendStrategy = () => _reinstallCalled = true;

        [SetUp]
        public void SetUp()
        {
            _originalReinstallSentryNativeBackendStrategy =
                Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy,
                    _fakeReinstallSentryNativeBackendStrategy);
            _reinstallCalled = false;
            _sentryUnityInfo = new TestSentryUnityInfo { IL2CPP = false };
        }

        [TearDown]
        public void TearDown() =>
            _fakeReinstallSentryNativeBackendStrategy =
                Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy!,
                    _originalReinstallSentryNativeBackendStrategy)!;

        [Test]
        public void Configure_DefaultConfiguration_SetsScopeObserver()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.IsAssignableFrom<AndroidJavaScopeObserver>(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_SetsCrashedLastRun()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.IsNotNull(options.CrashedLastRun);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.Null(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_EnablesScopeSync()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.False(options.EnableScopeSync);
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void Configure_IL2CPP_ReInitializesNativeBackend(bool il2cpp, bool expectedReinstall)
        {
            _sentryUnityInfo.IL2CPP = il2cpp;
            Assert.False(_reinstallCalled); // Sanity check

            SentryNativeAndroid.Configure(new(), _sentryUnityInfo);

            Assert.AreEqual(expectedReinstall, _reinstallCalled);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _sentryUnityInfo);
            Assert.False(_reinstallCalled);
        }
    }

    public class TestSentryUnityInfo : ISentryUnityInfo
    {
        public bool IL2CPP { get; set; }
        public string? Platform { get; }
        public Il2CppMethods? Il2CppMethods { get; }
        public bool IsKnownPlatform() => true;
    }
}
