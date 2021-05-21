using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Sentry.Unity.Tests
{
    public sealed class EmbeddedResourcesTests
    {
        [Test]
        public void LinkXml_Embedded()
        {
            var resourceNames = Assembly.GetAssembly(typeof(SentryUnity)).GetManifestResourceNames();

            Assert.NotNull(resourceNames);
            Assert.IsTrue(resourceNames.Contains("Sentry.Unity.xml"));
        }
    }
}
