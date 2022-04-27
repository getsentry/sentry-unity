using NUnit.Framework;

namespace Sentry.Unity.iOS.Tests
{
    public class SentryNativeCocoaTests
    {
        [Test]
        public void Configure_DefaultConfiguration_SetsScopeObserver()
        {
            var options = new SentryUnityOptions();
            SentryNativeCocoa.Configure(options);
            Assert.IsAssignableFrom<NativeScopeObserver>(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_SetsCrashedLastRun()
        {
            var options = new SentryUnityOptions();
            SentryNativeCocoa.Configure(options);
            Assert.IsNotNull(options.CrashedLastRun);
        }

        [Test]
        public void Configure_NativeIosSupportDisabled_ObserverIsNull()
        {
            var options = new SentryUnityOptions { IosNativeSupportEnabled = false };
            SentryNativeCocoa.Configure(options);
            Assert.Null(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_EnablesScopeSync()
        {
            var options = new SentryUnityOptions();
            SentryNativeCocoa.Configure(options);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeIosSupportDisabled_DisabledScopeSync()
        {
            var options = new SentryUnityOptions { IosNativeSupportEnabled = false };
            SentryNativeCocoa.Configure(options);
            Assert.False(options.EnableScopeSync);
        }
    }
}
