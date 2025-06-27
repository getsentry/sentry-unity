using System;
using System.ComponentModel;
using Sentry.Extensibility;

namespace Sentry.Unity;

/// <summary>
/// Sentry Unity initialization class.
/// </summary>
public static class SentryUnity
{
    private static SentryUnitySdk? UnitySdk;

    /// <summary>
    /// Initializes Sentry Unity SDK while configuring the options.
    /// </summary>
    /// <param name="sentryUnityOptionsConfigure">Callback to configure the options.</param>
    public static void Init(Action<SentryUnityOptions> sentryUnityOptionsConfigure)
    {
        var options = new SentryUnityOptions();
        sentryUnityOptionsConfigure.Invoke(options);

        Init(options);
    }

    /// <summary>
    /// Initializes Sentry Unity SDK while providing an options object.
    /// </summary>
    /// <param name="options">The options object.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Init(SentryUnityOptions options)
    {
        if (UnitySdk is not null)
        {
            options.DiagnosticLogger?.LogWarning("The SDK has already been initialized.");
        }

        UnitySdk = SentryUnitySdk.Init(options);
    }

    /// <summary>
    /// Closes the Sentry Unity SDK
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Close()
    {
        UnitySdk?.Close();
        UnitySdk = null;
    }

    /// <summary>
    /// Represents the crash state of the games's previous run.
    /// Used to determine if the last execution terminated normally or crashed.
    /// </summary>
    public enum CrashedLastRun
    {
        /// <summary>
        /// The LastRunState is unknown. This might be due to the SDK not being initialized, native crash support
        /// missing, or being disabled.
        /// </summary>
        Unknown,

        /// <summary>
        /// The application did not crash during the last run.
        /// </summary>
        DidNotCrash,

        /// <summary>
        /// The application crashed during the last run.
        /// </summary>
        Crashed
    }

    /// <summary>
    /// Retrieves the crash state of the previous application run.
    /// This indicates whether the application terminated normally or crashed.
    /// </summary>
    /// <returns><see cref="CrashedLastRun"/> indicating the state of the previous run.</returns>
    public static CrashedLastRun GetLastRunState()
    {
        if (UnitySdk is null)
        {
            return CrashedLastRun.Unknown;
        }

        return UnitySdk.CrashedLastRun();
    }

    /// <summary>
    /// Captures a User Feedback
    /// </summary>
    public static void CaptureFeedback(string message, string? email, string? name, bool addScreenshot) =>
        UnitySdk?.CaptureFeedback(message, email, name, addScreenshot);
}
