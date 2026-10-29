using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the periodic heartbeat used by sentry-native's app-hang detection.
/// The coroutine runs on the Unity main thread, which is the thread the native
/// daemon latches onto as the monitored target.
/// </summary>
public partial class SentryMonoBehaviour
{
    private static readonly TimeSpan AppHangHeartbeatInterval = TimeSpan.FromSeconds(1);

    private Coroutine? _appHangHeartbeat;
    private Action? _appHangPause;
    private Action? _appHangResume;
    private bool _appHangArmed;

    /// <summary>
    /// Starts the app-hang heartbeat on the main thread at a fixed 1-second interval. Arming is
    /// deferred until the player loop is running (see <see cref="AppHangHeartbeatCoroutine"/>) so
    /// the synchronous startup stall isn't reported as a hang.
    /// </summary>
    public Coroutine StartAppHangHeartbeat(Action heartbeat) =>
        StartAppHangHeartbeat(heartbeat, AppHangHeartbeatInterval);

    internal Coroutine StartAppHangHeartbeat(Action heartbeat, Action pause)
    {
        if (_appHangPause is not null)
        {
            ApplicationPausing -= _appHangPause;
        }

        if (_appHangResume is not null)
        {
            ApplicationResuming -= _appHangResume;
        }

        _appHangPause = pause;
        ApplicationPausing += pause;
        // Re-arms right away instead of on the next tick, so a hang right after resuming is caught.
        _appHangResume = () =>
        {
            if (_appHangArmed)
            {
                heartbeat();
            }
        };
        ApplicationResuming += _appHangResume;
        return StartAppHangHeartbeat(heartbeat, AppHangHeartbeatInterval);
    }

    // Internal overload so tests can use a short interval.
    internal Coroutine StartAppHangHeartbeat(Action heartbeat, TimeSpan interval)
    {
        if (_appHangHeartbeat is not null)
        {
            StopCoroutine(_appHangHeartbeat);
        }

        _appHangArmed = false;
        _appHangHeartbeat = StartCoroutine(AppHangHeartbeatCoroutine(heartbeat, interval));
        return _appHangHeartbeat;
    }

    private IEnumerator AppHangHeartbeatCoroutine(Action heartbeat, TimeSpan interval)
    {
        // Skipping the first frame. The first heartbeat both latches the main thread as the
        // monitored target and arms detection. The monitor no-op without having received a 
        // heartbeat. During startup, splash screen plus the first scene load routinely block the 
        // main thread longer than the hang timeout and would cause false positives.
        // This also works in batchmode/headless (e.g. LinuxServer), unlike WaitForEndOfFrame.
        yield return null;
        heartbeat();
        _appHangArmed = true;

        var wait = new WaitForSecondsRealtime((float)interval.TotalSeconds);
        while (true)
        {
            yield return wait;
            // Unity still renders a few frames after losing focus, and a heartbeat there would re-arm the paused detector.
            if (_isRunning)
            {
                heartbeat();
            }
        }
    }
}
