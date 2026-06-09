namespace Sentry.Unity;

/// <summary>
/// Selects the native backend used on Linux.
/// </summary>
public enum LinuxBackend
{
    /// <summary>
    /// Use the Breakpad backend (default). In-process crash handler; crashes are uploaded on the next launch.
    /// </summary>
    Breakpad,
    /// <summary>
    /// Use the sentry-native SDK's own out-of-process crash daemon (<c>sentry-crash</c>).
    /// Uploads crashes immediately.
    /// </summary>
    Native,
}
