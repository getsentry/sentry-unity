using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public sealed class EmbeddedResourcesTests
    {
        [Test]
        public void LinkXml_Embedded()
        {
            var resourceNames = typeof(SentryUnity).Assembly.GetManifestResourceNames();

            Assert.NotNull(resourceNames);
            Assert.Contains("Sentry.Unity.xml", resourceNames);
        }
    }
}
