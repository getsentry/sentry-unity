using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity.Tests.Stubs;

internal class TestSentryMonoBehaviour : MonoBehaviour, ISentryMonoBehaviour
{
    public event System.Action? ApplicationResuming;
    public event System.Action? ApplicationPausing;
    public void ResumeApplication() => ApplicationResuming?.Invoke();
    public void PauseApplication() => ApplicationPausing?.Invoke();

    public bool StartCoroutineCalled { get; private set; }
    public int StopCoroutineCallCount { get; private set; }

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        StartCoroutineCalled = true;
        return base.StartCoroutine(routine);
    }

    public new void StopCoroutine(Coroutine routine)
    {
        StopCoroutineCallCount++;
        base.StopCoroutine(routine);
    }

    public void QueueCoroutine(IEnumerator routine)
    {
        // For tests, assume we're on the main thread and start immediately
        StartCoroutine(routine);
    }
}
