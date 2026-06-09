using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the periodic heartbeat used by the native SDK's app-hang detection.
/// The coroutine runs on the Unity main thread, which is the thread the native
/// daemon latches onto as the monitored target (first caller wins).
/// </summary>
public partial class SentryMonoBehaviour
{
    private static readonly TimeSpan AppHangHeartbeatInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Starts the app-hang heartbeat on the main thread at a fixed 1-second interval. Arming is
    /// deferred until the player loop is running (see <see cref="AppHangHeartbeatCoroutine"/>) so
    /// the synchronous startup stall isn't reported as a hang.
    /// </summary>
    public Coroutine StartAppHangHeartbeat(Action heartbeat) =>
        StartAppHangHeartbeat(heartbeat, AppHangHeartbeatInterval);

    // Internal overload so tests can use a short interval.
    internal Coroutine StartAppHangHeartbeat(Action heartbeat, TimeSpan interval) =>
        StartCoroutine(AppHangHeartbeatCoroutine(heartbeat, interval));

    private IEnumerator AppHangHeartbeatCoroutine(Action heartbeat, TimeSpan interval)
    {
        // Defer arming by a frame. The first heartbeat both latches the main thread as the
        // monitored target and arms detection (the native side treats a missing heartbeat as "not
        // armed"). Startup - splash screen plus the first scene load - routinely blocks the main
        // thread longer than the hang timeout, so arming any earlier reports that startup stall as
        // a false hang. A single 'yield return null' suspends until the player loop ticks, by which
        // point the synchronous startup work is behind us. Unlike WaitForEndOfFrame this also
        // resumes in batchmode/headless (e.g. OSXServer), so detection still arms there.
        yield return null;
        heartbeat();

        // WaitForSecondsRealtime so a paused or Time.timeScale == 0 game keeps
        // heartbeating.
        var wait = new WaitForSecondsRealtime((float)interval.TotalSeconds);
        while (true)
        {
            yield return wait;
            heartbeat();
        }
    }
}
