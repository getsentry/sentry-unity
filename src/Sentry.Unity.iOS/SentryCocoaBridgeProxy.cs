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
        WriteOptions(options,
            (name, value) => OptionsSetString(cOptions, name, value),
            (name, value) => OptionsSetInt(cOptions, name, value),
            (name, value) => OptionsSetDouble(cOptions, name, value),
            (min, max) => OptionsAddFailedRequestStatusCodeRange(cOptions, min, max));

        var result = StartWithOptions(cOptions);
        return result is 1;
    }

    /// <summary>
    /// Writes the options sentry-cocoa reads from its options dictionary through the given setters.
    /// </summary>
    internal static void WriteOptions(SentryUnityOptions options,
        Action<string, string> setString,
        Action<string, int> setInt,
        Action<string, double> setDouble,
        Action<int, int> addFailedRequestStatusCodeRange)
    {
        // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
        setString("dsn", options.Dsn!);

        if (options.Release is not null)
        {
            Logger?.LogDebug("Setting Release: {0}", options.Release);
            setString("release", options.Release);
        }

        if (options.Environment is not null)
        {
            Logger?.LogDebug("Setting Environment: {0}", options.Environment);
            setString("environment", options.Environment);
        }

        Logger?.LogDebug("Setting Debug: {0}", options.Debug);
        setInt("debug", options.Debug ? 1 : 0);

        var diagnosticLevel = options.DiagnosticLevel.ToString().ToLowerInvariant();
        Logger?.LogDebug("Setting DiagnosticLevel: {0}", diagnosticLevel);
        setString("diagnosticLevel", diagnosticLevel);

        if (options.SampleRate.HasValue)
        {
            Logger?.LogDebug("Setting SampleRate: {0}", options.SampleRate.Value);
            setDouble("sampleRate", options.SampleRate.Value);
        }

        Logger?.LogDebug("Setting SendDefaultPii: {0}", options.SendDefaultPii);
        setInt("sendDefaultPii", options.SendDefaultPii ? 1 : 0);

        // macOS screenshots currently don't work, because there's no UIKit. Cocoa logs: "Sentry - info:: NO UIKit"
        // Logger?.LogDebug("Setting AttachScreenshot: {0}", options.AttachScreenshot);
        // setInt("attachScreenshot", options.AttachScreenshot ? 1 : 0);
        setInt("attachScreenshot", 0);

        Logger?.LogDebug("Setting MaxBreadcrumbs: {0}", options.MaxBreadcrumbs);
        setInt("maxBreadcrumbs", options.MaxBreadcrumbs);

        Logger?.LogDebug("Setting MaxCacheItems: {0}", options.MaxCacheItems);
        setInt("maxCacheItems", options.MaxCacheItems);

        // See https://github.com/getsentry/sentry-unity/issues/1658
        setInt("enableNetworkBreadcrumbs", 0);

        Logger?.LogDebug("Setting EnableAppHangTracking: {0}", options.EnableAppHangTracking);
        setInt("enableAppHangTracking", options.EnableAppHangTracking ? 1 : 0);

        Logger?.LogDebug("Setting AppHangTimeoutInterval: {0}s", options.AppHangTimeout.TotalSeconds);
        setDouble("appHangTimeoutInterval", options.AppHangTimeout.TotalSeconds);

        Logger?.LogDebug("Setting EnableWatchdogTerminationTracking: {0}", options.IosWatchdogTerminationIntegrationEnabled);
        setInt("enableWatchdogTerminationTracking", options.IosWatchdogTerminationIntegrationEnabled ? 1 : 0);

        Logger?.LogDebug("Setting CaptureFailedRequests: {0}", options.CaptureFailedRequests);
        setInt("enableCaptureFailedRequests", options.CaptureFailedRequests ? 1 : 0);

        Logger?.LogDebug("Setting EnableLogs: {0}", options.EnableLogs);
        setInt("enableLogs", options.EnableLogs ? 1 : 0);

        foreach (var range in options.FailedRequestStatusCodes)
        {
            Logger?.LogDebug("Adding FailedRequestStatusCodeRange: {0}-{1}", range.Start, range.End);
            addFailedRequestStatusCodeRange(range.Start, range.End);
        }
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

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsSetDouble")]
    private static extern void OptionsSetDouble(IntPtr options, string name, double value);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeOptionsAddFailedRequestStatusCodeRange")]
    private static extern void OptionsAddFailedRequestStatusCodeRange(IntPtr options, int min, int max);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeStartWithOptions")]
    private static extern int StartWithOptions(IntPtr options);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetSdkName")]
    public static extern int SetSdkName();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeCrashedLastRun")]
    public static extern int CrashedLastRun();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeClose")]
    public static extern void Close();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeAddBreadcrumb")]
    public static extern void AddBreadcrumb(string timestamp, string? message, string? type, string? category, int level,
        string[]? dataKeys, string[]? dataValues, int dataCount);

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

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetConnection")]
    private static extern void GetConnection(out string? type, out string? effectiveType);

    public static (string? Type, string? EffectiveType) GetConnection()
    {
        GetConnection(out var type, out var effectiveType);
        return (type, effectiveType);
    }

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetOption")]
    public static extern string? GetOption(string name);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetTraceHeader")]
    public static extern string? GetTraceHeader();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetBaggageHeader")]
    public static extern string? GetBaggageHeader();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeGetUser")]
    public static extern string? GetUser();

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetTrace")]
    public static extern void SetTrace(string traceId, string spanId);

    [DllImport("__Internal", EntryPoint = "SentryNativeBridgeSetEnvironment")]
    public static extern void SetEnvironment(string? environment);
}
