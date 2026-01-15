using System;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests;

[TestFixture]
public class ContentBasedThrottlerTests
{
    [Test]
    public void ShouldCapture_FirstCall_ReturnsTrue()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        var result = throttler.ShouldCapture("test message", "stacktrace", LogType.Error);

        Assert.IsTrue(result);
    }

    [Test]
    public void ShouldCapture_SameMessageWithinWindow_ReturnsFalse()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";
        var stackTrace = "stacktrace";

        throttler.ShouldCapture(message, stackTrace, LogType.Error);
        var result = throttler.ShouldCapture(message, stackTrace, LogType.Error);

        Assert.IsFalse(result);
    }

    [Test]
    public void ShouldCapture_DifferentMessages_BothReturnTrue()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        var result1 = throttler.ShouldCapture("message 1", "stacktrace", LogType.Error);
        var result2 = throttler.ShouldCapture("message 2", "stacktrace", LogType.Error);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCapture_SameMessageDifferentStackTrace_BothReturnTrue()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        var result1 = throttler.ShouldCapture("message", "stacktrace 1", LogType.Error);
        var result2 = throttler.ShouldCapture("message", "stacktrace 2", LogType.Error);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCapture_LogTypeLog_AlwaysReturnsTrue()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCapture(message, "stacktrace", LogType.Log);
        var result2 = throttler.ShouldCapture(message, "stacktrace", LogType.Log);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCapture_LogTypeWarning_AlwaysReturnsTrue()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCapture(message, "stacktrace", LogType.Warning);
        var result2 = throttler.ShouldCapture(message, "stacktrace", LogType.Warning);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCapture_LogTypeException_ThrottlesRepeated()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCapture(message, "stacktrace", LogType.Exception);
        var result2 = throttler.ShouldCapture(message, "stacktrace", LogType.Exception);

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [Test]
    public void ShouldCapture_BufferFull_EvictsOldest()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10), maxBufferSize: 2);

        // Fill buffer
        throttler.ShouldCapture("message 1", "stack", LogType.Error);
        throttler.ShouldCapture("message 2", "stack", LogType.Error);

        // This should evict "message 1"
        throttler.ShouldCapture("message 3", "stack", LogType.Error);

        // "message 1" should now be allowed again since it was evicted
        var result = throttler.ShouldCapture("message 1", "stack", LogType.Error);

        Assert.IsTrue(result);
    }

    [Test]
    public void ShouldCapture_NullStackTrace_DoesNotThrow()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        Assert.DoesNotThrow(() => throttler.ShouldCapture("message", null!, LogType.Error));
    }

    [Test]
    public void ShouldCapture_EmptyStackTrace_DoesNotThrow()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        Assert.DoesNotThrow(() => throttler.ShouldCapture("message", string.Empty, LogType.Error));
    }
}
