using System;
using System.Runtime.InteropServices;
using Sentry.Extensibility;

namespace Sentry.Unity.iOS;

/// <summary>
/// P/Invoke to SentryNativeBridge.m which communicates with the `sentry-cocoa` SDK.
/// </summary>
/// <remarks>
/// Functions are declared in `SentryNativeBridge.m`
/// </remarks>
/// <see href="https://github.com/getsentry/sentry-cocoa"/>
internal static class SentryCocoaBridgeProxy
{
    private static IDiagnosticLogger? Logger;
    public static bool IsEnabled() => SentryNativeBridgeIsEnabled() == 1;

    public static bool Init(SentryUnityOptions options)
    {
        if (LoadLibrary() != 1)
        {
            return false;
        }

        Logger = options.DiagnosticLogger;

        var cOptions = OptionsNew();

        // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
        OptionsSetString(cOptions, "dsn", options.Dsn!);

        if (options.Release is not null)
        {
            Logger?.LogDebug("Setting Release: {0}", options.Release);
            OptionsSetString(cOptions, "release", options.Release);
        }

        if (options.Environment is not null)
        {
            Logger?.LogDebug("Setting Environment: {0}", options.Environment);
            OptionsSetString(cOptions, "environment", options.Environment);
        }

        Logger?.LogDebug("Setting Debug: {0}", options.Debug);
        OptionsSetInt(cOptions, "debug", options.Debug ? 1 : 0);

        var diagnosticLevel = options.DiagnosticLevel.ToString().ToLowerInvariant();
        Logger?.LogDebug("Setting DiagnosticLevel: {0}", diagnosticLevel);
        OptionsSetString(cOptions, "diagnosticLevel", diagnosticLevel);

        Logger?.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
        OptionsSetInt(cOptions, "sendDefaultPii", options.SendDefaultPii ? 1 : 0);

        // macOS screenshots currently don't work, because there's no UIKit. Cocoa logs: "Sentry - info:: NO UIKit"
        // Logger?.LogDebug("Setting AttachScreenshot: {0}", options.AttachScreenshot);
        // OptionsSetInt(cOptions, "attachScreenshot", options.AttachScreenshot ? 1 : 0);
        OptionsSetInt(cOptions, "attachScreenshot", 0);

        Logger?.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
        OptionsSetInt(cOptions, "maxBreadcrumbs", options.MaxBreadcrumbs);

        Logger?.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
        OptionsSetInt(cOptions, "maxCacheItems", options.MaxCacheItems);

        // See https://github.com/getsentry/sentry-unity/issues/1658
        OptionsSetInt(cOptions, "enableNetworkBreadcrumbs", 0);

        Logger?.LogDebug("Setting EnableWatchdogTerminationTracking: {0}", options.IosWatchdogTerminationIntegrationEnabled);
        OptionsSetInt(cOptions, "enableWatchdogTerminationTracking", options.IosWatchdogTerminationIntegrationEnabled ? 1 : 0);

        var result = StartWithOptions(cOptions);
        return result is 1;
    }

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeLoadLibrary")]
    private static extern int LoadLibrary();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeIsEnabled")]
    private static extern int SentryNativeBridgeIsEnabled();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsNew")]
    private static extern IntPtr OptionsNew();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsSetString")]
    private static extern void OptionsSetString(IntPtr options, string name, string value);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsSetInt")]
    private static extern void OptionsSetInt(IntPtr options, string name, int value);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeStartWithOptions")]
    private static extern int StartWithOptions(IntPtr options);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetSdkName")]
    public static extern int SetSdkName();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeCrashedLastRun")]
    public static extern int CrashedLastRun();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeClose")]
    public static extern void Close();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeAddBreadcrumb")]
    public static extern void AddBreadcrumb(string timestamp, string? message, string? type, string? category, int level);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetExtra")]
    public static extern void SetExtra(string key, string? value);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetTag")]
    public static extern void SetTag(string key, string value);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeUnsetTag")]
    public static extern void UnsetTag(string key);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetUser")]
    public static extern void SetUser(string? email, string? userId, string? ipAddress, string? username);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeUnsetUser")]
    public static extern void UnsetUser();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetInstallationId")]
    public static extern string GetInstallationId();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetTrace")]
    public static extern void SetTrace(string traceId, string spanId);
}
