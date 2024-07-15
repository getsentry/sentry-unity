using NUnit.Framework;
using Sentry.Unity.Editor.ConfigurationWindow;

namespace Sentry.Unity.Tests;

public sealed class EmbeddedResourcesTests
{
    [Test]
    public void Resources_Embedded()
    {
        var resourceNames = typeof(SentryWindow).Assembly.GetManifestResourceNames();

        Assert.NotNull(resourceNames);
        Assert.Contains("Sentry.Unity.Editor.Resources.SentryLogoLight.png", resourceNames);
        Assert.Contains("Sentry.Unity.Editor.Resources.SentryLogoDark.png", resourceNames);
    }
}
