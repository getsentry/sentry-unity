using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Content-based throttler that deduplicates events based on message and stack trace fingerprint.
/// Only throttles LogType.Error, LogType.Exception, and LogType.Assert events.
/// </summary>
internal class ContentBasedThrottler : IErrorEventThrottler
{
    private readonly Dictionary<int, LinkedListNode<LruEntry>> _cache = new();
    private readonly LinkedList<LruEntry> _accessOrder = new();
    private readonly object _lock = new();
    private readonly int _maxBufferSize;
    private readonly TimeSpan _dedupeWindow;

    private readonly struct LruEntry
    {
        public readonly int Hash;
        public readonly DateTimeOffset Timestamp;

        public LruEntry(int hash, DateTimeOffset timestamp)
        {
            Hash = hash;
            Timestamp = timestamp;
        }
    }

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
        // Only throttle Error, Exception, and Assert
        if (logType is not (LogType.Error or LogType.Exception or LogType.Assert))
        {
            return true;
        }

        var hash = ComputeHash(message, stackTrace);
        return ShouldCaptureByHash(hash);
    }

    /// <summary>
    /// Checks if an exception should be captured without allocating a fingerprint string.
    /// Computes hash directly from exception type name + message + stack trace.
    /// </summary>
    internal bool ShouldCaptureException(Exception exception)
    {
        var hash = ComputeExceptionHash(exception);
        return ShouldCaptureByHash(hash);
    }

    private bool ShouldCaptureByHash(int hash)
    {
        var now = DateTimeOffset.UtcNow;

        lock (_lock)
        {
            if (_cache.TryGetValue(hash, out var existingNode))
            {
                // Entry exists - check if still within dedupe window
                if (now - existingNode.Value.Timestamp < _dedupeWindow)
                {
                    return false; // Throttle - seen recently
                }

                // Entry expired - update timestamp and move to end (most recently used)
                _accessOrder.Remove(existingNode);
                var newNode = _accessOrder.AddLast(new LruEntry(hash, now));
                _cache[hash] = newNode;
                return true; // Allow capture
            }

            // New entry - evict oldest if buffer is full
            if (_cache.Count >= _maxBufferSize)
            {
                EvictOldest();
            }

            // Add new entry at end (most recently used)
            var node = _accessOrder.AddLast(new LruEntry(hash, now));
            _cache[hash] = node;
            return true; // Allow capture
        }
    }

    private static int ComputeExceptionHash(Exception exception)
    {
        // Compute hash from exception type name + ":" + message + stack trace
        // without allocating a combined string
        var typeName = exception.GetType().Name;
        var message = exception.Message;
        var stackTrace = exception.StackTrace;

        // Hash the type name
        var hash = typeName?.GetHashCode() ?? 0;

        // Add separator ":"
        hash = hash * 31 + ':';

        // Add message hash
        hash = hash * 31 + (message?.GetHashCode() ?? 0);

        // Add stack trace prefix hash
        if (!string.IsNullOrEmpty(stackTrace))
        {
            hash = hash * 31 + ComputeStackTraceHash(stackTrace!, 200);
        }

        return hash;
    }

    private static int ComputeHash(string message, string? stackTrace)
    {
        // Start with message hash
        var hash = message?.GetHashCode() ?? 0;

        // Combine with stack trace prefix hash using multiplicative combining (not XOR)
        // Process character-by-character to avoid Substring allocation
        if (!string.IsNullOrEmpty(stackTrace))
        {
            var stackTraceHash = ComputeStackTraceHash(stackTrace!, 200);
            hash = hash * 31 + stackTraceHash;
        }

        return hash;
    }

    private static int ComputeStackTraceHash(string stackTrace, int maxLength)
    {
        var length = Math.Min(stackTrace.Length, maxLength);
        var hash = 17;
        for (var i = 0; i < length; i++)
        {
            hash = hash * 31 + stackTrace[i];
        }
        return hash;
    }

    private void EvictOldest()
    {
        // O(1) eviction - remove from head of linked list
        var oldest = _accessOrder.First;
        if (oldest != null)
        {
            _cache.Remove(oldest.Value.Hash);
            _accessOrder.RemoveFirst();
        }
    }
}
