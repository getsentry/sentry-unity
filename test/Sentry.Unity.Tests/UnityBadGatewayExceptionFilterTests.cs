using System;
using NUnit.Framework;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Tests;

public class UnityBadGatewayExceptionFilterTests
{
    [Test]
    public void Filter_FiltersBadGatewayExceptionsOfTypeException() =>
        Assert.IsTrue(new UnityBadGatewayExceptionFilter().Filter(new Exception(UnityBadGatewayExceptionFilter.Message)));

    [Test]
    public void Filter_ReturnsFalse_ForOtherExceptions() =>
        Assert.IsFalse(new UnityBadGatewayExceptionFilter().Filter(new Exception("Some other error message")));
}
