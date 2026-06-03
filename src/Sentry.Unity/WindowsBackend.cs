namespace Sentry.Unity;

/// <summary>
/// Selects the native backend used on Windows.
/// </summary>
public enum WindowsBackend
{
    /// <summary>
    /// Use the Crashpad backend (default). Ships <c>crashpad_handler.exe</c> as the out-of-process handler.
    /// </summary>
    Crashpad,
    /// <summary>
    /// Use the sentry-native SDK's own out-of-process crash daemon (<c>sentry-crash.exe</c>).
    /// Uploads crashes immediately.
    /// </summary>
    Native,
}
