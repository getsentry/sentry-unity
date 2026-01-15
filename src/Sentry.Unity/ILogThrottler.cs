using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Interface for throttling log events to prevent quota exhaustion from high-frequency errors.
/// </summary>
/// <remarks>
/// Throttling only affects event capture - breadcrumbs and structured logs are not affected.
/// </remarks>
public interface ILogThrottler
{
    /// <summary>
    /// Determines whether a log message should be captured as an event.
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="stackTrace">Stack trace for fingerprinting</param>
    /// <param name="logType">Unity LogType</param>
    /// <returns>True if the event should be captured, false to throttle</returns>
    bool ShouldCapture(string message, string stackTrace, LogType logType);
}
