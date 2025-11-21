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

    internal static bool IsNativeSupportEnabled(this SentryUnityOptions options, RuntimePlatform? platform = null)
    {
        platform ??= ApplicationAdapter.Instance.Platform;
        return platform switch
        {
            RuntimePlatform.Android => options.AndroidNativeSupportEnabled,
            RuntimePlatform.IPhonePlayer => options.IosNativeSupportEnabled,
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsServer => options.WindowsNativeSupportEnabled,
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXServer => options.MacosNativeSupportEnabled,
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxServer => options.LinuxNativeSupportEnabled,
            RuntimePlatform.GameCoreXboxSeries or RuntimePlatform.GameCoreXboxOne => options.XboxNativeSupportEnabled,
            RuntimePlatform.PS4 or RuntimePlatform.PS5 => options.PlayStationNativeSupportEnabled,
            _ => false
        };
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
    /// Disables the capture of logs, warnings, errors, breadcrumbs, and structured logs through
    /// <see cref="UnityApplicationLoggingIntegration"/>.
    /// </summary>
    /// <param name="options">The SentryUnityOptions to remove the integration from.</param>
    public static void DisableUnityLoggingIntegration(this SentryUnityOptions options) =>
        options.RemoveIntegration<UnityApplicationLoggingIntegration>();

    /// <summary>
    /// Disables the capture of unhandled exceptions. This removes both <see cref="UnityLogHandlerIntegration"/>
    /// (non-WebGL platforms) and <see cref="UnityWebGLExceptionHandler"/> (WebGL platform).
    /// </summary>
    /// <param name="options">The SentryUnityOptions to remove the integration from.</param>
    public static void DisableUnhandledExceptionCapture(this SentryUnityOptions options)
    {
        options.RemoveIntegration<UnityLogHandlerIntegration>();
        options.RemoveIntegration<UnityWebGLExceptionHandler>();
    }

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
