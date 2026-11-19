using System;
using Sentry.Extensibility;

namespace Sentry.Unity.iOS;

/// <summary>
/// The session replay sentry-cocoa records.
/// </summary>
internal interface ICocoaReplay
{
    void Start();
    void StartBuffering();
    void Pause();
    void Resume();
    void Stop();

    /// <summary>
    /// Captures the replay for an error. Returns whether the error belongs to a replay.
    /// </summary>
    bool Capture();

    /// <summary>
    /// The replay id on the native scope, or null when no replay is sent.
    /// </summary>
    string? GetReplayId();

    /// <summary>
    /// Lets Unity capture the frames of every replay started afterwards.
    /// </summary>
    void UseUnityCapture();

    /// <summary>
    /// Takes the frame request sentry-cocoa made, with the frame's size in points. Returns the request's id, or 0 when
    /// there's none.
    /// </summary>
    int TakeFrameRequest(out int width, out int height);

    /// <summary>
    /// Completes a request with a frame of top-down RGBA pixels. A request that was cancelled in the meantime isn't
    /// completed again.
    /// </summary>
    void DeliverFrame(int request, IntPtr rgba, int width, int height);

    /// <summary>
    /// Completes a pending request with the last frame.
    /// </summary>
    void CancelFrameRequest();
}

internal sealed class CocoaReplayBridge : ICocoaReplay
{
    public void Start() => SentryCocoaBridgeProxy.ReplayStart();
    public void StartBuffering() => SentryCocoaBridgeProxy.ReplayStartBuffering();
    public void Pause() => SentryCocoaBridgeProxy.ReplayPause();
    public void Resume() => SentryCocoaBridgeProxy.ReplayResume();
    public void Stop() => SentryCocoaBridgeProxy.ReplayStop();
    public bool Capture() => SentryCocoaBridgeProxy.ReplayCapture() == 1;
    public string? GetReplayId() => SentryCocoaBridgeProxy.GetReplayId();
    public void UseUnityCapture() => SentryCocoaBridgeProxy.ReplayUseUnityCapture();

    public int TakeFrameRequest(out int width, out int height) =>
        SentryCocoaBridgeProxy.ReplayTakeFrameRequest(out width, out height);

    public void DeliverFrame(int request, IntPtr rgba, int width, int height) =>
        SentryCocoaBridgeProxy.ReplayDeliverFrame(request, rgba, width, height);

    public void CancelFrameRequest() => SentryCocoaBridgeProxy.ReplayCancelFrameRequest();
}

/// <summary>
/// Samples, starts and stops sentry-cocoa's session replay with the Unity application's lifecycle.
/// </summary>
/// <remarks>
/// The game's sentry-cocoa doesn't track sessions, so its replay doesn't start by itself: the replay is sampled here
/// when the SDK starts, and again after a background longer than the session interval, which starts a new session.
/// </remarks>
internal sealed class CocoaReplayLifecycle
{
    private readonly SentryUnityOptions _options;
    private readonly ICocoaReplay _replay;
    private readonly Func<double> _random;
    private readonly SentryMonoBehaviour _monoBehaviour;
    private readonly UnityReplayRecorder? _recorder;
    private DateTime? _pausedAt;

    private CocoaReplayLifecycle(SentryUnityOptions options, ICocoaReplay replay, Func<double> random,
        SentryMonoBehaviour monoBehaviour, UnityReplayRecorder? recorder)
    {
        _options = options;
        _replay = replay;
        _random = random;
        _monoBehaviour = monoBehaviour;
        _recorder = recorder;
    }

    public static void Use(SentryUnityOptions options, ICocoaReplay replay, Func<double>? random = null)
    {
        UnityReplayRecorder? recorder = null;
        if (options.Experimental.SessionReplay.Capture is SessionReplayCapture.Unity)
        {
            options.DiagnosticLogger?.LogDebug("Session replay records the frames Unity captures.");
            replay.UseUnityCapture();
            recorder = new UnityReplayRecorder(replay, options.DiagnosticLogger);
        }

        var randomGenerator = new Random();
        var lifecycle = new CocoaReplayLifecycle(options, replay, random ?? randomGenerator.NextDouble,
            SentryMonoBehaviour.Instance, recorder);
        options.ReplaySession = new CocoaReplaySession(replay);
        recorder?.Start(lifecycle._monoBehaviour);
        lifecycle.Start();

        lifecycle._monoBehaviour.ApplicationPausing += lifecycle.OnPausing;
        lifecycle._monoBehaviour.ApplicationResuming += lifecycle.OnResuming;
        options.NativeSupportCloseCallback += lifecycle.Close;
    }

    private void Start()
    {
        var replayOptions = _options.Experimental.SessionReplay;
        if (_random() < replayOptions.SessionSampleRate)
        {
            _options.DiagnosticLogger?.LogDebug("Recording the session replay.");
            _replay.Start();
        }
        else if (replayOptions.OnErrorSampleRate > 0)
        {
            _options.DiagnosticLogger?.LogDebug("Buffering the session replay for errors.");
            _replay.StartBuffering();
        }
        else
        {
            _options.DiagnosticLogger?.LogDebug("The session replay is not sampled.");
        }
    }

    private void OnPausing()
    {
        _pausedAt = DateTime.UtcNow;
        _recorder?.Pause();
        _replay.Pause();
    }

    private void OnResuming()
    {
        var newSession = _pausedAt is { } pausedAt && DateTime.UtcNow - pausedAt >= _options.AutoSessionTrackingInterval;
        _pausedAt = null;
        _recorder?.Resume();
        if (!newSession)
        {
            _replay.Resume();
            return;
        }

        // Like a new session, a long background ends the replay at the pause and samples a new one.
        _options.DiagnosticLogger?.LogDebug("Ending the session replay after a long background.");
        _replay.Stop();
        Start();
    }

    private void Close()
    {
        _monoBehaviour.ApplicationPausing -= OnPausing;
        _monoBehaviour.ApplicationResuming -= OnResuming;
        _recorder?.Stop();
        _replay.Stop();
    }
}
