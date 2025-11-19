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
    public void RunJniSafe_OnMainThread_ExecutesAction()
    {
        // Arrange
        var actionExecuted = false;
        var action = new Action(() => actionExecuted = true);

        // Act
        _sut.RunJniSafe(action, isMainThread: true);

        // Assert
        Assert.That(actionExecuted, Is.True);
    }

    [Test]
    public void RunJniSafe_NotMainThread_ExecutesOnWorkerThread()
    {
        // Arrange
        var actionExecuted = false;
        var executionThreadId = 0;
        var resetEvent = new ManualResetEvent(false);
        var action = new Action(() =>
        {
            actionExecuted = true;
            executionThreadId = Thread.CurrentThread.ManagedThreadId;
            resetEvent.Set();
        });

        // Act
        _sut.RunJniSafe(action, isMainThread: false);

        // Assert
        Assert.That(resetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
        Assert.That(actionExecuted, Is.True);
        Assert.That(executionThreadId, Is.Not.EqualTo(Thread.CurrentThread.ManagedThreadId),
            "Action should execute on worker thread, not the calling thread");
    }

    [Test]
    public void RunJniSafe_ActionThrowsOnMainThread_CatchesExceptionAndLogsError()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var action = new Action(() => throw exception);

        // Act
        _sut.RunJniSafe(action, "TestAction", isMainThread: true);

        // Assert
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
        Assert.That(resetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True);
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Calling 'TestAction' failed.")));
    }

    [Test]
    public void WorkerThread_AttachesOnCreationAndDetachesOnClose()
    {
        // Arrange
        var logger = new TestLogger();
        var androidJni = new TestAndroidJNI();
        var attachResetEvent = new ManualResetEvent(false);
        var detachResetEvent = new ManualResetEvent(false);
        androidJni.OnAttachCalled = () => attachResetEvent.Set();
        androidJni.OnDetachCalled = () => detachResetEvent.Set();

        // Act - Create instance (should start worker thread and attach)
        var sut = new SentryJava(logger, androidJni);

        // Trigger worker thread initialization by queuing work
        var workExecuted = new ManualResetEvent(false);
        sut.RunJniSafe(() => workExecuted.Set(), isMainThread: false);

        // Assert - Worker thread should be attached
        Assert.That(workExecuted.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "Work should execute to initialize worker thread");
        Assert.That(attachResetEvent.WaitOne(TimeSpan.FromSeconds(1)), Is.True, "AttachCurrentThread should be called on worker thread creation");
        Assert.That(androidJni.AttachCalled, Is.True);

        // Act - Close/Dispose (should stop worker thread and detach)
        sut.Close();

        // Assert - Worker thread should be detached
        Assert.That(detachResetEvent.WaitOne(TimeSpan.FromSeconds(2)), Is.True, "DetachCurrentThread should be called when closing");
        Assert.That(androidJni.DetachCalled, Is.True);
    }

    [Test]
    public void RunJniSafe_AfterClose_SkipsActionAndLogsWarning()
    {
        // Arrange
        var actionExecuted = false;
        _sut.Close();

        // Act
        _sut.RunJniSafe(() => actionExecuted = true, "TestAction", isMainThread: false);

        // Assert
        Assert.That(actionExecuted, Is.False, "Action should not execute after Close()");
        Assert.That(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Info &&
            log.message.Contains("Scope sync is closed, skipping 'TestAction'")),
            Is.True,
            "Should log warning when trying to queue action after Close()");
    }

    internal class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }
        public Action? OnAttachCalled { get; set; }
        public Action? OnDetachCalled { get; set; }

        public void AttachCurrentThread()
        {
            AttachCalled = true;
            OnAttachCalled?.Invoke();
        }

        public void DetachCurrentThread()
        {
            DetachCalled = true;
            OnDetachCalled?.Invoke();
        }
    }
}
