using System;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Interface for log message deduplication.
/// </summary>
public interface IUnityLogMessageDebounce
{
    /// <summary>
    /// Checks if a log message should be debounced based on its content.
    /// Returns true if the message should be allowed through, false if it should be blocked.
    /// </summary>
    bool Debounced(string message, string stacktrace, LogType logType);
}

/// <summary>
/// Content-based debounce that deduplicates log messages based on their content hash.
/// This class is not thread-safe and is designed to be called by Unity non-threaded logger callback.
/// </summary>
public class ContentDebounce : IUnityLogMessageDebounce
{
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    private readonly struct LogEntry
    {
        public readonly int Hash;
        public readonly DateTimeOffset Timestamp;

        public LogEntry(int hash, DateTimeOffset timestamp)
        {
            Hash = hash;
            Timestamp = timestamp;
        }
    }

    private readonly TimeSpan _debounceWindow;
    private readonly LogEntry[] _ringBuffer;
    private int _head;

    public ContentDebounce(TimeSpan debounceWindow, int bufferSize = 100)
    {
        _debounceWindow = debounceWindow;
        _ringBuffer = new LogEntry[bufferSize];
        _head = 0;
    }

    /// <summary>
    /// Checks if the log content should be debounced.
    /// Returns true if the message should be allowed through, false if it should be blocked.
    /// </summary>
    public bool Debounced(string message, string stacktrace, LogType logType)
    {
        var contentHash = HashCode.Combine(message, stacktrace);
        var currentTime = Now;

        foreach (var entry in _ringBuffer)
        {
            if (entry.Hash != contentHash || entry.Timestamp == default)
            {
                continue;
            }

            var timeSinceLastSeen = currentTime - entry.Timestamp;
            if (timeSinceLastSeen < _debounceWindow)
            {
                return false;
            }
        }

        _ringBuffer[_head] = new LogEntry(contentHash, currentTime);
        _head = (_head + 1) % _ringBuffer.Length;
        return true;
    }
}
