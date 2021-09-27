using NUnit.Framework;

namespace Sentry.Unity.iOS.Tests
{
    public class SentryNativeIosTests
    {
        [Test]
        public void Configure_DefaultConfiguration_SetsScopeObserver()
        {
            var options = new SentryUnityOptions();
            SentryNativeIos.Configure(options);
            Assert.IsAssignableFrom<IosNativeScopeObserver>(options.ScopeObserver);
        }

        [Test]
        public void Configure_NativeIosSupportDisabled_ObserverIsNull()
        {
            var options = new SentryUnityOptions { IosNativeSupportEnabled = false };
            SentryNativeIos.Configure(options);
            Assert.Null(options.ScopeObserver);
        }

        [Test]
        public void Configure_DefaultConfiguration_EnablesScopeSync()
        {
            var options = new SentryUnityOptions();
            SentryNativeIos.Configure(options);
            Assert.True(options.EnableScopeSync);
        }

        [Test]
        public void Configure_NativeIosSupportDisabled_DisabledScopeSync()
        {
            var options = new SentryUnityOptions { IosNativeSupportEnabled = false };
            SentryNativeIos.Configure(options);
            Assert.False(options.EnableScopeSync);
        }
    }
}
