using NUnit.Framework;
using Sentry;

public class SentryTestsBase
{
    [SetUp]
    public void SetUp()
    {
        SentrySdk.AddBreadcrumb("Running Test: " + TestContext.CurrentContext.Test.Name);
    }
}
