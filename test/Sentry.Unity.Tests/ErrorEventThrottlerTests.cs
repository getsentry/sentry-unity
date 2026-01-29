using System;
using NUnit.Framework;
using UnityEngine;

namespace Sentry.Unity.Tests;

[TestFixture]
public class ErrorEventThrottlerTests
{
    [Test]
    public void ShouldCaptureEvent_FirstCall_ReturnsTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        var result = throttler.ShouldCaptureEvent("test message", "stacktrace", LogType.Error);

        Assert.IsTrue(result);
    }

    [Test]
    public void ShouldCaptureEvent_SameMessageWithinWindow_ReturnsFalse()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";
        var stackTrace = "stacktrace";

        throttler.ShouldCaptureEvent(message, stackTrace, LogType.Error);
        var result = throttler.ShouldCaptureEvent(message, stackTrace, LogType.Error);

        Assert.IsFalse(result);
    }

    [Test]
    public void ShouldCaptureEvent_DifferentMessages_BothReturnTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        var result1 = throttler.ShouldCaptureEvent("message 1", "stacktrace", LogType.Error);
        var result2 = throttler.ShouldCaptureEvent("message 2", "stacktrace", LogType.Error);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureEvent_SameMessageDifferentStackTrace_BothReturnTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        var result1 = throttler.ShouldCaptureEvent("message", "stacktrace 1", LogType.Error);
        var result2 = throttler.ShouldCaptureEvent("message", "stacktrace 2", LogType.Error);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureEvent_LogTypeLog_AlwaysReturnsTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Log);
        var result2 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Log);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureEvent_LogTypeWarning_AlwaysReturnsTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Warning);
        var result2 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Warning);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureEvent_LogTypeException_ThrottlesRepeated()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var message = "test message";

        var result1 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Exception);
        var result2 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Exception);

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [Test]
    public void ShouldCaptureEvent_BufferFull_EvictsOldest()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10), maxBufferSize: 2);

        // Fill buffer
        throttler.ShouldCaptureEvent("message 1", "stack", LogType.Error);
        throttler.ShouldCaptureEvent("message 2", "stack", LogType.Error);

        // This should evict "message 1"
        throttler.ShouldCaptureEvent("message 3", "stack", LogType.Error);

        // "message 1" should now be allowed again since it was evicted
        var result = throttler.ShouldCaptureEvent("message 1", "stack", LogType.Error);

        Assert.IsTrue(result);
    }

    [Test]
    public void ShouldCaptureEvent_NullStackTrace_DoesNotThrow()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        Assert.DoesNotThrow(() => throttler.ShouldCaptureEvent("message", null!, LogType.Error));
    }

    [Test]
    public void ShouldCaptureEvent_UpdatingExpiredEntry_DoesNotEvict()
    {
        // Use a buffer of 3 to avoid cascading evictions affecting our test
        var throttler = new ErrorEventThrottler(TimeSpan.FromMilliseconds(50), maxBufferSize: 3);

        // Fill buffer with entries A, B, and C
        throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);
        throttler.ShouldCaptureEvent("message B", "stack", LogType.Error);
        throttler.ShouldCaptureEvent("message C", "stack", LogType.Error);

        // Wait for entries to expire
        System.Threading.Thread.Sleep(60);

        // Update expired entry A - should NOT evict, just update timestamp
        // If the bug existed, this would evict B, reducing buffer to 2
        throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);

        // Add new entry D - this should evict B (the oldest after A was refreshed)
        // Buffer should now contain: A, C, D
        throttler.ShouldCaptureEvent("message D", "stack", LogType.Error);

        // B was evicted so should be allowed again
        var resultB = throttler.ShouldCaptureEvent("message B", "stack", LogType.Error);
        // A was updated (not evicted), so should be throttled (timestamp was refreshed)
        var resultA = throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);

        Assert.IsTrue(resultB, "Entry B should have been evicted and allowed again");
        Assert.IsFalse(resultA, "Entry A should still be in buffer and throttled");
    }

    [Test]
    public void ShouldCaptureEvent_EmptyStackTrace_DoesNotThrow()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        Assert.DoesNotThrow(() => throttler.ShouldCaptureEvent("message", string.Empty, LogType.Error));
    }

    [Test]
    public void ShouldCaptureEvent_LogTypeAssert_ThrottlesRepeated()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var message = "assertion failed";

        var result1 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Assert);
        var result2 = throttler.ShouldCaptureEvent(message, "stacktrace", LogType.Assert);

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [Test]
    public void ShouldCaptureEvent_LruEviction_EvictsLeastRecentlyUsed()
    {
        // Buffer size of 3 to avoid cascading evictions affecting assertions
        var throttler = new ErrorEventThrottler(TimeSpan.FromMilliseconds(50), maxBufferSize: 3);

        // Add A, B, and C - fills buffer
        // Access order: A, B, C
        throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);
        throttler.ShouldCaptureEvent("message B", "stack", LogType.Error);
        throttler.ShouldCaptureEvent("message C", "stack", LogType.Error);

        // Wait for expiry
        System.Threading.Thread.Sleep(60);

        // Access A again (makes A most recently used)
        // Access order after: B, C, A
        throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);

        // Add D - should evict B (least recently used)
        // Access order after: C, A, D
        throttler.ShouldCaptureEvent("message D", "stack", LogType.Error);

        // B was evicted, should be allowed (re-adds B, evicts C)
        // Access order after: A, D, B
        var resultB = throttler.ShouldCaptureEvent("message B", "stack", LogType.Error);
        // A was refreshed and not evicted, should be throttled
        var resultA = throttler.ShouldCaptureEvent("message A", "stack", LogType.Error);

        Assert.IsTrue(resultB, "Entry B should have been evicted (LRU) and allowed again");
        Assert.IsFalse(resultA, "Entry A should still be in buffer and throttled");
    }

    [Test]
    public void ShouldCaptureException_ThrottlesRepeatedExceptions()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var exception = new InvalidOperationException("test error");

        var result1 = throttler.ShouldCaptureException(exception);
        var result2 = throttler.ShouldCaptureException(exception);

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }

    [Test]
    public void ShouldCaptureException_DifferentExceptionTypes_BothAllowed()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var exception1 = new InvalidOperationException("test error");
        var exception2 = new ArgumentException("test error");

        var result1 = throttler.ShouldCaptureException(exception1);
        var result2 = throttler.ShouldCaptureException(exception2);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureException_SameTypeDifferentMessage_BothAllowed()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var exception1 = new InvalidOperationException("error 1");
        var exception2 = new InvalidOperationException("error 2");

        var result1 = throttler.ShouldCaptureException(exception1);
        var result2 = throttler.ShouldCaptureException(exception2);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureBreadcrumb_AlwaysReturnsTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        var breadcrumb = new Breadcrumb("test message", "test.category");

        // Default implementation should never throttle breadcrumbs
        var result1 = throttler.ShouldCaptureBreadcrumb(breadcrumb);
        var result2 = throttler.ShouldCaptureBreadcrumb(breadcrumb);

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void ShouldCaptureLog_AlwaysReturnsTrue()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));

        // Default implementation should never throttle structured logs
        var result1 = throttler.ShouldCaptureStructuredLog(SentryLevel.Error, "test message");
        var result2 = throttler.ShouldCaptureStructuredLog(SentryLevel.Error, "test message");

        Assert.IsTrue(result1);
        Assert.IsTrue(result2);
    }

    [Test]
    public void IThrottler_ShouldCaptureException_ThrottlesRepeated()
    {
        var throttler = new ErrorEventThrottler(TimeSpan.FromSeconds(10));
        IThrottler iThrottler = throttler;
        var exception = new InvalidOperationException("test error");

        var result1 = iThrottler.ShouldCaptureException(exception);
        var result2 = iThrottler.ShouldCaptureException(exception);

        Assert.IsTrue(result1);
        Assert.IsFalse(result2);
    }
}
