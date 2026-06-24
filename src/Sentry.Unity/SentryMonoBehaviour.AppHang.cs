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
        // Skipping the first frame. The first heartbeat both latches the main thread as the
        // monitored target and arms detection. The monitor no-op without having received a 
        // heartbeat. During startup, splash screen plus the first scene load routinely block the 
        // main thread longer than the hang timeout and would cause false positives.
        // This also works in batchmode/headless (e.g. LinuxServer), unlike WaitForEndOfFrame.
        yield return null;
        heartbeat();

        var wait = new WaitForSecondsRealtime((float)interval.TotalSeconds);
        while (true)
        {
            yield return wait;
            heartbeat();
        }
    }
}
