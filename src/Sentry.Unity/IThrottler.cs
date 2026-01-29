using System;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Interface for throttling events, breadcrumbs, logs, and exceptions to prevent quota exhaustion.
/// </summary>
/// <remarks>
/// The default implementation (<see cref="ErrorEventThrottler"/>) only throttles error/exception events.
/// Users can implement custom throttlers to also throttle breadcrumbs, logs, etc.
/// </remarks>
public interface IThrottler
{
    /// <summary>
    /// Determines whether an error event should be captured.
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="stackTrace">Stack trace for fingerprinting</param>
    /// <param name="logType">Unity LogType</param>
    /// <returns>True if the event should be captured, false to throttle</returns>
    bool ShouldCaptureEvent(string message, string stackTrace, LogType logType);

    /// <summary>
    /// Determines whether a breadcrumb should be recorded.
    /// </summary>
    /// <param name="message">The breadcrumb message to check</param>
    /// /// <param name="logType">Unity LogType</param>
    /// <returns>True if the breadcrumb should be recorded, false to throttle</returns>
    bool ShouldCaptureBreadcrumb(string message, LogType logType);

    /// <summary>
    /// Determines whether a log message should be captured (structured logging).
    /// </summary>
    /// <param name="message">The log message</param>
    /// <param name="logType">Unity LogType</param>
    /// <returns>True if the log should be captured, false to throttle</returns>
    bool ShouldCaptureStructuredLog(string message, LogType logType);

    /// <summary>
    /// Determines whether an exception should be captured.
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the exception should be captured, false to throttle</returns>
    bool ShouldCaptureException(Exception exception);
}
