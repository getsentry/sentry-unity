using NUnit.Framework;
using Sentry.Unity.NativeUtils;

namespace Sentry.Unity.Tests;

[SetUpFixture]
public class SentryPlatformServicesTests
{
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        SentryPlatformServices.UnityInfo = new TestUnityInfo();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {

    }
}
