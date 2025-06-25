using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

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
    public void RunJniSafe_OnMainThread_ExecutesActionWithoutAttachingDetaching()
    {
        // Arrange
        var actionExecuted = false;
        var action = new Action(() => actionExecuted = true);

        // Act
        _sut.RunJniSafe(action, isMainThread: true);

        // Assert
        Assert.That(actionExecuted, Is.True);
        Assert.That(_androidJni.AttachCalled, Is.False); // Sanity Check
        Assert.That(_androidJni.DetachCalled, Is.False); // Sanity Check
    }

    [Test]
    public void RunJniSafe_NotMainThread_ExecutesOnThreadPool()
    {
        // Arrange
        var actionExecuted = false;
        var resetEvent = new ManualResetEvent(false);
        var detachResetEvent = new ManualResetEvent(false);
        _androidJni.OnDetachCalled = () => detachResetEvent.Set();
        var action = new Action(() =>
        {
            actionExecuted = true;
            resetEvent.Set();
        });

        // Act
        _sut.RunJniSafe(action, isMainThread: false);

        // Assert
        Assert.That(resetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Action should execute within timeout");
        Assert.That(detachResetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Detach should execute within timeout");
        Assert.That(actionExecuted, Is.True);
        Assert.That(_androidJni.AttachCalled, Is.True);
        Assert.That(_androidJni.DetachCalled, Is.True, "Detach should be called");
    }

    [Test]
    public void RunJniSafe_ActionThrowsOnMainThread_CatchesExceptionAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var action = new Action(() => throw exception);

        // Act
        _sut.RunJniSafe(action, "TestAction", isMainThread: true);

        Assert.That(_androidJni.AttachCalled, Is.False);
        Assert.That(_androidJni.DetachCalled, Is.False);
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Calling 'TestAction' failed.")));
    }

    [Test]
    public void RunJniSafe_ActionThrowsOnNonMainThread_CatchesExceptionAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var resetEvent = new ManualResetEvent(false);
        var detachResetEvent = new ManualResetEvent(false);
        _androidJni.OnDetachCalled = () => detachResetEvent.Set();
        var action = new Action(() =>
        {
            try
            {
                throw exception;
            }
            finally
            {
                resetEvent.Set();
            }
        });

        // Act
        _sut.RunJniSafe(action, "TestAction", isMainThread: false);

        // Assert
        Assert.That(resetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Action should execute within timeout");
        Assert.That(detachResetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Detach should execute within timeout");
        Assert.That(_androidJni.AttachCalled, Is.True);
        Assert.That(_androidJni.DetachCalled, Is.True);
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Calling 'TestAction' failed.")));
    }

    [Test]
    public void RunJniSafe_ActionThrowsOnNonMainThread_DetachIsAlwaysCalled()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var resetEvent = new ManualResetEvent(false);
        var detachResetEvent = new ManualResetEvent(false);
        _androidJni.OnDetachCalled = () => detachResetEvent.Set();
        var action = new Action(() =>
        {
            try
            {
                throw exception;
            }
            finally
            {
                resetEvent.Set();
            }
        });

        // Act
        _sut.RunJniSafe(action, isMainThread: false);

        // Assert
        Assert.That(resetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Action should execute within timeout");
        Assert.That(detachResetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Detach should execute within timeout");
        Assert.That(_androidJni.AttachCalled, Is.True);
        Assert.That(_androidJni.DetachCalled, Is.True, "DetachCurrentThread should always be called");
    }

    internal class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }
        public Action? OnDetachCalled { get; set; }

        public void AttachCurrentThread() => AttachCalled = true;

        public void DetachCurrentThread()
        {
            DetachCalled = true;
            OnDetachCalled?.Invoke();
        }
    }
}
