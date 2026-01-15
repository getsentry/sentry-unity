using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Content-based throttler that deduplicates events based on message and stack trace fingerprint.
/// Only throttles LogType.Error and LogType.Exception events.
/// </summary>
internal class ContentBasedThrottler : ILogThrottler
{
    private readonly Dictionary<int, DateTimeOffset> _hashTimestamps = new();
    private readonly int _maxBufferSize;
    private readonly TimeSpan _dedupeWindow;

    /// <summary>
    /// Creates a new content-based throttler.
    /// </summary>
    /// <param name="dedupeWindow">Time window for deduplicating repeated errors with the same fingerprint.</param>
    /// <param name="maxBufferSize">Maximum number of fingerprints to track. Oldest entries are evicted when full.</param>
    public ContentBasedThrottler(TimeSpan dedupeWindow, int maxBufferSize = 100)
    {
        _dedupeWindow = dedupeWindow;
        _maxBufferSize = maxBufferSize;
    }

    /// <inheritdoc />
    public bool ShouldCapture(string message, string stackTrace, LogType logType)
    {
        // Only throttle Error and Exception
        if (logType is not (LogType.Error or LogType.Exception))
        {
            return true;
        }

        var hash = ComputeHash(message, stackTrace);
        var now = DateTimeOffset.UtcNow;

        if (_hashTimestamps.TryGetValue(hash, out var lastSeen))
        {
            if (now - lastSeen < _dedupeWindow)
            {
                return false; // Throttle - seen recently
            }
        }

        // LRU eviction if buffer full
        if (_hashTimestamps.Count >= _maxBufferSize)
        {
            EvictOldest();
        }

        _hashTimestamps[hash] = now;
        return true; // Allow capture
    }

    private static int ComputeHash(string message, string? stackTrace)
    {
        var hash = message.GetHashCode();
        if (!string.IsNullOrEmpty(stackTrace))
        {
            // Use first ~200 chars of stack trace for performance
            var stackTracePrefix = stackTrace!.Length > 200
                ? stackTrace.Substring(0, 200)
                : stackTrace;
            hash ^= stackTracePrefix.GetHashCode();
        }
        return hash;
    }

    private void EvictOldest()
    {
        if (_hashTimestamps.Count == 0)
        {
            return;
        }

        var oldest = _hashTimestamps.OrderBy(kvp => kvp.Value).First().Key;
        _hashTimestamps.Remove(oldest);
    }
}
