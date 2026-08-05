using System;
using System.Collections;
using System.Collections.Generic;
using Sentry.Unity.Metrics;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the sampling for the auto-collected game performance metrics.
/// </summary>
public partial class SentryMonoBehaviour
{
    private readonly Dictionary<Type, Coroutine> _metricCoroutines = new();

    internal void StartMetricsMonitor(IGameMetricMonitor monitor, TimeSpan? interval = null)
    {
        var monitorType = monitor.GetType();
        if (_metricCoroutines.TryGetValue(monitorType, out var coroutine))
        {
            StopCoroutine(coroutine);
        }

        _metricCoroutines[monitorType] = StartCoroutine(MetricsCoroutine(monitor, interval));
    }

    private static IEnumerator MetricsCoroutine(IGameMetricMonitor monitor, TimeSpan? interval)
    {
        var wait = interval.HasValue
            ? new WaitForSecondsRealtime((float)interval.Value.TotalSeconds)
            : null;

        while (true)
        {
            yield return wait;
            monitor.Sample();
        }
    }
}
