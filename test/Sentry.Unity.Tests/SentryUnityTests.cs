using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public class SentryUnityTests
    {
        [Test]
        public void SentryUnity_OptionsValid_Initializes()
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);
            options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";

            SentryUnity.Init(options);

            Assert.IsTrue(SentrySdk.IsEnabled);
        }

        [Test]
        public void SentryUnity_OptionsInvalid_DoesNotInitialize()
        {
            var options = new SentryUnityOptions();
            SentryOptionsUtility.SetDefaults(options);

            // Even tho the defaults are set the DSN is missing making the options invalid for initialization
            SentryUnity.Init(options);

            Assert.IsFalse(SentrySdk.IsEnabled);
        }
    }
}
