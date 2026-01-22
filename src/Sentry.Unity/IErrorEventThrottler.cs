using System;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Interface for throttling error and exception events to prevent quota exhaustion from high-frequency errors.
/// </summary>
/// <remarks>
/// Throttling only affects error/exception event capture - breadcrumbs and structured logs are not affected.
/// </remarks>
public interface IErrorEventThrottler
{
    /// <summary>
    /// Determines whether an error or exception should be captured as a Sentry event.
    /// </summary>
    /// <param name="message">The error message or exception fingerprint</param>
    /// <param name="stackTrace">Stack trace for fingerprinting</param>
    /// <param name="logType">Unity LogType (Error, Exception, or Assert)</param>
    /// <returns>True if the event should be captured, false to throttle</returns>
    bool ShouldCapture(string message, string stackTrace, LogType logType);
}

/// <summary>
/// Extension methods for <see cref="IErrorEventThrottler"/>.
/// </summary>
internal static class ErrorEventThrottlerExtensions
{
    /// <summary>
    /// Determines whether an exception should be captured as a Sentry event.
    /// Uses an allocation-free path when the throttler is <see cref="ContentBasedThrottler"/>.
    /// </summary>
    /// <param name="throttler">The throttler instance</param>
    /// <param name="exception">The exception to check</param>
    /// <returns>True if the event should be captured, false to throttle</returns>
    public static bool ShouldCaptureException(this IErrorEventThrottler throttler, Exception exception)
    {
        // Use allocation-free path for ContentBasedThrottler
        if (throttler is ContentBasedThrottler contentBasedThrottler)
        {
            return contentBasedThrottler.ShouldCaptureException(exception);
        }

        // Fallback for custom implementations - requires string allocation
        var fingerprint = $"{exception.GetType().Name}:{exception.Message}";
        return throttler.ShouldCapture(fingerprint, exception.StackTrace ?? string.Empty, LogType.Exception);
    }
}
