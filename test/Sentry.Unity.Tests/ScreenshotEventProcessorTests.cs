using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public class ScreenshotEventProcessorTests
{
    private class Fixture
    {
        public SentryUnityOptions Options = new() { AttachScreenshot = true };
        public TestApplication TestApplication = new();

        public ScreenshotEventProcessor GetSut() => new(Options);
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.Close();
        }
    }

    // Todo: Add tests that verify passing the capture on to the MonoBehaviour
}
