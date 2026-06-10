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
    /// Starts the app-hang heartbeat on the main thread at a fixed 1-second interval.
    /// </summary>
    public Coroutine StartAppHangHeartbeat(Action heartbeat) =>
        StartAppHangHeartbeat(heartbeat, AppHangHeartbeatInterval);

    // Internal overload so tests can use a short interval.
    internal Coroutine StartAppHangHeartbeat(Action heartbeat, TimeSpan interval) =>
        StartCoroutine(AppHangHeartbeatCoroutine(heartbeat, interval));

    private IEnumerator AppHangHeartbeatCoroutine(Action heartbeat, TimeSpan interval)
    {
        // Fire immediately to latch the main thread as the monitored target
        // before any real hang can occur.
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
