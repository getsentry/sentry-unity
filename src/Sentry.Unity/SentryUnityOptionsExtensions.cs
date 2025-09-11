using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity;

public static class SentryUnityOptionsExtensions
{
    public static bool ShouldInitializeSdk(this SentryUnityOptions? options) => ShouldInitializeSdk(options, null);

    internal static bool ShouldInitializeSdk(this SentryUnityOptions? options, IApplication? application = null)
    {
        if (options is null)
        {
            return false;
        }

        if (!IsValid(options))
        {
            return false;
        }

        application ??= ApplicationAdapter.Instance;
        if (!options!.CaptureInEditor && application.IsEditor)
        {
            options.DiagnosticLogger?.LogInfo("Disabled while in the Editor.");
            return false;
        }

        return true;
    }

    internal static bool IsValid(this SentryUnityOptions options)
    {
        if (!options.Enabled)
        {
            options.DiagnosticLogger?.LogDebug("Sentry SDK has been disabled." +
                                               "\nYou can disable this log by raising the debug verbosity level above 'Debug'.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Dsn))
        {
            options.DiagnosticLogger?.LogWarning("No Sentry DSN configured. Sentry will be disabled.");
            return false;
        }

        return true;
    }

    internal static bool IsNativeSupportEnabled(this SentryUnityOptions options, IApplication? application = null)
    {
        application ??= ApplicationAdapter.Instance;
        switch (application.Platform)
        {
            case RuntimePlatform.Android:
                return options.AndroidNativeSupportEnabled;
            case RuntimePlatform.IPhonePlayer:
                return options.IosNativeSupportEnabled;
            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsServer:
                return options.WindowsNativeSupportEnabled;
            case RuntimePlatform.OSXPlayer:
            case RuntimePlatform.OSXServer:
                return options.MacosNativeSupportEnabled;
            case RuntimePlatform.LinuxPlayer:
            case RuntimePlatform.LinuxServer:
                return options.LinuxNativeSupportEnabled;
            default:
                return false;
        }
    }

    internal static void SetupUnityLogging(this SentryUnityOptions options)
    {
        if (options.Debug)
        {
            if (options.DiagnosticLogger is null)
            {
                options.DiagnosticLogger = new UnityLogger(options);
                options.DiagnosticLogger.LogDebug("Logging enabled with 'UnityLogger' min level: {0}", options.DiagnosticLevel);
            }
        }
        else
        {
            options.DiagnosticLogger = null;
        }
    }

    /// <summary>
    /// Disables the capture of errors through <see cref="UnityLogHandlerIntegration"/>.
    /// </summary>
    /// <param name="options">The SentryUnityOptions to remove the integration from.</param>
    public static void DisableUnityApplicationLoggingIntegration(this SentryUnityOptions options) =>
        options.RemoveIntegration<UnityLogHandlerIntegration>();

    /// <summary>
    /// Disables the application-not-responding detection.
    /// </summary>
    public static void DisableAnrIntegration(this SentryUnityOptions options) =>
        options.RemoveIntegration<AnrIntegration>();

    /// <summary>
    /// Disables the automatic filtering of Bad Gateway exception of type Exception.
    /// </summary>
    public static void DisableBadGatewayExceptionFilter(this SentryUnityOptions options) =>
        options.RemoveExceptionFilter<UnityBadGatewayExceptionFilter>();

    /// <summary>
    /// Disables the automatic filtering of System.Net.WebException.
    /// </summary>
    public static void DisableWebExceptionFilter(this SentryUnityOptions options) =>
        options.RemoveExceptionFilter<UnityWebExceptionFilter>();

    /// <summary>
    /// Disables the automatic filtering of System.Net.Sockets.SocketException with error code 10049.
    /// </summary>
    public static void DisableSocketExceptionFilter(this SentryUnityOptions options) =>
        options.RemoveExceptionFilter<UnitySocketExceptionFilter>();
}
