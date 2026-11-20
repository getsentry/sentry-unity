namespace Sentry.Unity;

/// <summary>
/// Session replay on Android, recorded by the Android SDK (sentry-java). Off unless one of the sample rates is above 0.
/// Experimental: set these from code, in the options configuration used both at build time and at runtime. The build
/// adds the replay library to the Android project only when replay is enabled there. Replay needs the Android SDK
/// initialized at runtime (<see cref="NativeInitializationType.Runtime"/>).
/// </summary>
public class ExperimentalSessionReplayOptions
{
    /// <summary>
    /// The share of sessions that are recorded in full, from 0 to 1. Defaults to 0.
    /// </summary>
    public double SessionSampleRate { get; set; }

    /// <summary>
    /// The share of errors that send the replay of the moments before them, from 0 to 1. Defaults to 0.
    /// </summary>
    public double OnErrorSampleRate { get; set; }

    /// <summary>
    /// What captures the replay's frames. Defaults to <see cref="SessionReplayCapture.SentryJava"/>.
    /// </summary>
    public SessionReplayCapture Capture { get; set; } = SessionReplayCapture.SentryJava;

    /// <summary>
    /// The replay's video quality. Defaults to <see cref="SessionReplayQuality.Medium"/>.
    /// </summary>
    public SessionReplayQuality Quality { get; set; } = SessionReplayQuality.Medium;

    internal bool IsEnabled => SessionSampleRate > 0 || OnErrorSampleRate > 0;
}

/// <summary>
/// What captures the frames of a session replay.
/// </summary>
public enum SessionReplayCapture
{
    /// <summary>
    /// The Android SDK copies the screen, including Unity's surface, at the replay's frame rate.
    /// </summary>
    SentryJava,

    /// <summary>
    /// Unity captures its own frames and hands them to the Android SDK.
    /// </summary>
    Unity,
}

/// <summary>
/// The video quality of a session replay. Higher quality sends larger replays.
/// </summary>
public enum SessionReplayQuality
{
    /// <summary>
    /// Scaled down video at a low bit rate.
    /// </summary>
    Low,

    /// <summary>
    /// Full size video at a medium bit rate.
    /// </summary>
    Medium,

    /// <summary>
    /// Full size video at a high bit rate.
    /// </summary>
    High,
}
