using System;
using System.Threading;
using NUnit.Framework;

namespace Sentry.Unity.Android.Tests
{
    public class SentryNativeAndroidTests
    {
        private bool _reinstallCalled;
        private Action? _originalReinstallSentryNativeBackendStrategy;
        private Action _fakeReinstallSentryNativeBackendStrategy;
        private bool _il2cpp;

        public SentryNativeAndroidTests()
            => _fakeReinstallSentryNativeBackendStrategy = () => _reinstallCalled = true;

        [SetUp]
        public void SetUp()
        {
            _originalReinstallSentryNativeBackendStrategy =
                Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy,
                    _fakeReinstallSentryNativeBackendStrategy);
            _reinstallCalled = false;
            _il2cpp = false;
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
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.IsAssignableFrom<AndroidJavaScopeObserver>(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_SetsCrashedLastRun()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.IsNotNull(options.CrashedLastRun);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.Null(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_EnablesScopeSync()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.False(options.EnableScopeSync);
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Configure_IL2CPP_ReInitializesNativeBackendOnlyOnIL2CPP(bool il2cpp, bool expectedReinstall)
        {
            _il2cpp = il2cpp;
            Assert.False(_reinstallCalled); // Sanity check

            SentryNativeAndroid.Configure(new(), _il2cpp);

            Assert.AreEqual(expectedReinstall, _reinstallCalled);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options, _il2cpp);
            Assert.False(_reinstallCalled);
        }
    }
}
