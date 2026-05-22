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
    /// </summary>
    [field: SerializeField] public MacosBackend MacosBackend { get; set; } = MacosBackend.Cocoa;

    /// <summary>
    /// Selects the native backend to use on Windows. Defaults to <see cref="Sentry.Unity.WindowsBackend.Crashpad"/>,
    /// which ships <c>crashpad_handler.exe</c> as the out-of-process handler. Use
    /// <see cref="Sentry.Unity.WindowsBackend.Native"/> for sentry-native's new out-of-process <c>sentry-crash.exe</c>
    /// daemon, which uploads crashes immediately.
    /// </summary>
    [field: SerializeField] public WindowsBackend WindowsBackend { get; set; } = WindowsBackend.Crashpad;
}
