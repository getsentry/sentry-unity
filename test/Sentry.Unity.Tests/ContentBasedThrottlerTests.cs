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
    public void ShouldCapture_UpdatingExpiredEntry_DoesNotEvict()
    {
        // Use a buffer of 3 to avoid cascading evictions affecting our test
        var throttler = new ContentBasedThrottler(TimeSpan.FromMilliseconds(50), maxBufferSize: 3);

        // Fill buffer with entries A, B, and C
        throttler.ShouldCapture("message A", "stack", LogType.Error);
        throttler.ShouldCapture("message B", "stack", LogType.Error);
        throttler.ShouldCapture("message C", "stack", LogType.Error);

        // Wait for entries to expire
        System.Threading.Thread.Sleep(60);

        // Update expired entry A - should NOT evict, just update timestamp
        // If the bug existed, this would evict B, reducing buffer to 2
        throttler.ShouldCapture("message A", "stack", LogType.Error);

        // Add new entry D - this should evict B (the oldest after A was refreshed)
        // Buffer should now contain: A, C, D
        throttler.ShouldCapture("message D", "stack", LogType.Error);

        // B was evicted so should be allowed again
        var resultB = throttler.ShouldCapture("message B", "stack", LogType.Error);
        // A was updated (not evicted), so should be throttled (timestamp was refreshed)
        var resultA = throttler.ShouldCapture("message A", "stack", LogType.Error);

        Assert.IsTrue(resultB, "Entry B should have been evicted and allowed again");
        Assert.IsFalse(resultA, "Entry A should still be in buffer and throttled");
    }

    [Test]
    public void ShouldCapture_EmptyStackTrace_DoesNotThrow()
    {
        var throttler = new ContentBasedThrottler(TimeSpan.FromSeconds(10));

        Assert.DoesNotThrow(() => throttler.ShouldCapture("message", string.Empty, LogType.Error));
    }
}
