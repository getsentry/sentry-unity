using System;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Experimental options. APIs in this group may change without notice and their default values
/// may change between releases. Set the values you depend on explicitly.
/// </summary>
[Serializable]
public class ExperimentalSentryUnityOptions
{
    /// <summary>
    /// Selects the native backend to use on macOS. Defaults to <see cref="Sentry.Unity.MacosBackend.Cocoa"/>,
    /// which requires IL2CPP. Use <see cref="Sentry.Unity.MacosBackend.Native"/> for the out-of-process
    /// sentry-native backend, which uploads crashes immediately and supports both IL2CPP and Mono.
    /// When set to <see cref="Sentry.Unity.MacosBackend.Native"/>, <c>ShutdownTimeout</c> is raised to
    /// a minimum of 10 seconds so the out-of-process handler has time to flush before the player exits.
    /// </summary>
    [field: SerializeField] public MacosBackend MacosBackend { get; set; } = MacosBackend.Cocoa;

    /// <summary>
    /// Selects the native backend to use on Windows. Defaults to <see cref="Sentry.Unity.WindowsBackend.Crashpad"/>,
    /// which ships <c>crashpad_handler.exe</c> as the out-of-process handler. Use
    /// <see cref="Sentry.Unity.WindowsBackend.Native"/> for sentry-native's new out-of-process <c>sentry-crash.exe</c>
    /// daemon, which uploads crashes immediately.
    /// When set to <see cref="Sentry.Unity.WindowsBackend.Native"/>, <c>ShutdownTimeout</c> is raised to
    /// a minimum of 10 seconds so the out-of-process handler has time to flush before the player exits.
    /// </summary>
    [field: SerializeField] public WindowsBackend WindowsBackend { get; set; } = WindowsBackend.Crashpad;

    /// <summary>
    /// Selects the native backend to use on Linux. Defaults to <see cref="Sentry.Unity.LinuxBackend.Breakpad"/>,
    /// the in-process handler that uploads crashes on the next launch. Use
    /// <see cref="Sentry.Unity.LinuxBackend.Native"/> for sentry-native's new out-of-process <c>sentry-crash</c>
    /// daemon, which uploads crashes immediately.
    /// When set to <see cref="Sentry.Unity.LinuxBackend.Native"/>, <c>ShutdownTimeout</c> is raised to
    /// a minimum of 10 seconds so the out-of-process handler has time to flush before the player exits.
    /// </summary>
    [field: SerializeField] public LinuxBackend LinuxBackend { get; set; } = LinuxBackend.Breakpad;

    /// <summary>
    /// Enables app hang detection via <c>sentry-native</c> on macOS, Windows, Linux, and Android. Defaults to
    /// <c>false</c>. Requires the backend to be switched to <see cref="Sentry.Unity.MacosBackend.Native"/>
    /// on macOS. On Android it is routed through the NDK integration. <c>sentry-native</c> monitors the main thread and
    /// produces an app hang event including a stack trace. When enabled, the C# watchdog is skipped to avoid
    /// duplicate reports. The timeout is taken from <c>AppHangTimeout</c>.
    /// </summary>
    [field: SerializeField] public bool EnableNativeAppHangTracking { get; set; } = false;
}
