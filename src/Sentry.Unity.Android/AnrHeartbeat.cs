using System;
using System.Collections;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

/// <summary>
/// Drives a per-frame-ish heartbeat from the Unity main thread into sentry-java,
/// so sentry-java's ANR watchdog can detect when the Unity main thread is stuck.
/// </summary>
internal class AnrHeartbeat
{
    private readonly ISentryMonoBehaviour _monoBehaviour;
    private readonly ISentryJava _sentryJava;
    private readonly IDiagnosticLogger? _logger;
    private readonly float _intervalSeconds;
    private Coroutine? _coroutine;

    public AnrHeartbeat(
        ISentryMonoBehaviour monoBehaviour,
        ISentryJava sentryJava,
        TimeSpan anrTimeout,
        IDiagnosticLogger? logger = null)
    {
        _monoBehaviour = monoBehaviour;
        _sentryJava = sentryJava;
        _logger = logger;
        _intervalSeconds = Math.Max(0.001f, (float)(anrTimeout.TotalSeconds / 5));
    }

    public void Start()
    {
        if (_coroutine != null)
        {
            _logger?.LogDebug("ANR heartbeat already started; ignoring duplicate Start().");
            return;
        }

        _coroutine = _monoBehaviour.StartCoroutine(Loop());
        _monoBehaviour.ApplicationPausing += OnPause;
        _monoBehaviour.ApplicationResuming += OnResume;
        _logger?.LogDebug("ANR heartbeat started with interval {0}s", _intervalSeconds);
    }

    public void Stop()
    {
        _monoBehaviour.ApplicationPausing -= OnPause;
        _monoBehaviour.ApplicationResuming -= OnResume;

        if (_coroutine != null)
        {
            _monoBehaviour.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _logger?.LogDebug("ANR heartbeat stopped.");
    }

    internal void Beat() => _sentryJava.NotifyAnrThreadAlive();

    private IEnumerator Loop()
    {
        var wait = new WaitForSecondsRealtime(_intervalSeconds);
        while (true)
        {
            Beat();
            yield return wait;
        }
    }

    private void OnPause()
    {
        if (_coroutine != null)
        {
            _monoBehaviour.StopCoroutine(_coroutine);
            _coroutine = null;
        }
    }

    private void OnResume()
    {
        if (_coroutine == null)
        {
            _coroutine = _monoBehaviour.StartCoroutine(Loop());
        }
    }
}
