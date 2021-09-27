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

        public SentryNativeAndroidTests()
            => _fakeReinstallSentryNativeBackendStrategy = () => _reinstallCalled = true;

        [SetUp]
        public void SetUp()
        {
            _originalReinstallSentryNativeBackendStrategy =
                Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy,
                    _fakeReinstallSentryNativeBackendStrategy);
            _reinstallCalled = false;
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
            SentryNativeAndroid.Configure(options);
            Assert.IsAssignableFrom<AndroidJavaScopeObserver>(options.ScopeObserver);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options);
            Assert.Null(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_EnablesScopeSync()
        {
            var options = new SentryUnityOptions();
            SentryNativeAndroid.Configure(options);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options);
            Assert.False(options.EnableScopeSync);
        }

        [Test]
        public void Configure_DefaultConfiguration_ReInitializesNativeBackend()
        {
            Assert.False(_reinstallCalled); // Sanity check
            SentryNativeAndroid.Configure(new());
            Assert.True(_reinstallCalled);
        }

        [Test]
        public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
        {
            var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };
            SentryNativeAndroid.Configure(options);
            Assert.False(_reinstallCalled);
        }
    }
}
