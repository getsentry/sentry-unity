using System;

namespace Sentry.Unity;

public interface IUnityLogMessageDebounce
{
    bool Debounced();
}

/// <summary>
/// This class is not thread-safe and is designed to be called by Unity non-threaded logger callback
/// </summary>
internal class TimeDebounceBase : IUnityLogMessageDebounce
{
    private static DateTimeOffset Now => DateTimeOffset.UtcNow;

    protected TimeSpan DebounceOffset;

    private DateTimeOffset? _barrierOffset;

    public bool Debounced()
    {
        if (_barrierOffset != null && Now < _barrierOffset)
        {
            return false;
        }

        _barrierOffset = Now.Add(DebounceOffset);
        return true;
    }
}

/// <summary>
/// This class is not thread-safe and is designed to be called by Unity non-threaded logger callback
/// </summary>
internal sealed class LogTimeDebounce : TimeDebounceBase
{
    public LogTimeDebounce(TimeSpan debounceOffset) => DebounceOffset = debounceOffset;
}

/// <summary>
/// This class is not thread-safe and is designed to be called by Unity non-threaded logger callback
/// </summary>
internal sealed class ErrorTimeDebounce : TimeDebounceBase
{
    public ErrorTimeDebounce(TimeSpan debounceOffset) => DebounceOffset = debounceOffset;
}

/// <summary>
/// This class is not thread-safe and is designed to be called by Unity non-threaded logger callback
/// </summary>
internal sealed class WarningTimeDebounce : TimeDebounceBase
{
    public WarningTimeDebounce(TimeSpan debounceOffset) => DebounceOffset = debounceOffset;
}