namespace Sentry.Unity;

/// <summary>
/// Selects the native backend used on macOS.
/// </summary>
public enum MacosBackend
{
    /// <summary>
    /// Use the Sentry Cocoa SDK (default). Requires IL2CPP.
    /// </summary>
    Cocoa,
    /// <summary>
    /// Use the sentry-native SDK. Runs out-of-process and uploads crashes immediately. Supports both IL2CPP and Mono.
    /// </summary>
    Native,
}
