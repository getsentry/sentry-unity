using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using System;
using System.Linq;

namespace Sentry.Unity.Android.Tests;

public class SentryJavaTests
{
    private TestLogger _logger = null!;
    private TestAndroidJNI _androidJni = null!;
    private SentryJava _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _logger = new TestLogger();
        _androidJni = new TestAndroidJNI();
        _sut = new SentryJava(_logger, _androidJni);
    }

    [Test]
    [TestCase(true, false, Description = "main thread = should attach")]
    [TestCase(false, true, Description = "non main thread = should not attach")]
    public void HandleJniThreadAttachment_AttachesIfMainThread(bool isMainThread, bool shouldAttach)
    {
        // Act
        _sut.HandleJniThreadAttachment(isMainThread);

        // Assert
        Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
    }

    [Test]
    [TestCase(true, false, Description = "main thread = should detach")]
    [TestCase(false, true, Description = "non main thread = should not detach")]
    public void HandleJniThreadDetachment_DetachesIfMainThread(bool isMainThread, bool shouldAttach)
    {
        // Act
        _sut.HandleJniThreadDetachment(isMainThread);

        // Assert
        Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
    }

    [Test]
    public void SafeExecuteJniOperation_WithFunc_ExecutesActionAndReturnsResult()
    {
        // Arrange
        const string expectedResult = "test-result";

        // Act
        var result = _sut.SafeExecuteJniOperation(() => expectedResult);

        // Assert
        Assert.AreEqual(expectedResult, result);
    }

    [Test]
    public void SafeExecuteJniOperation_WithFunc_HandlesException()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        var result = _sut.SafeExecuteJniOperation<string>(() => throw exception);

        // Assert
        Assert.IsNull(result);
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("JNI operation calling 'SafeExecuteJniOperation_WithFunc_HandlesException' failed.")));
    }

    [Test]
    public void SafeExecuteJniOperation_WithAction_ExecutesAction()
    {
        // Arrange
        var actionExecuted = false;

        // Act
        _sut.SafeExecuteJniOperation(() => actionExecuted = true);

        // Assert
        Assert.IsTrue(actionExecuted);
    }

    [Test]
    public void SafeExecuteJniOperation_WithAction_HandlesException()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        _sut.SafeExecuteJniOperation(() => throw exception);

        // Assert
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("JNI operation calling 'SafeExecuteJniOperation_WithAction_HandlesException' failed.")));
    }

    internal class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public void AttachCurrentThread() => AttachCalled = true;

        public void DetachCurrentThread() => DetachCalled = true;
    }
}
