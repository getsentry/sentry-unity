using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity.Tests.Stubs;

internal class TestSentryMonoBehaviour : MonoBehaviour, ISentryMonoBehaviour
{
    public event System.Action? ApplicationResuming;
    public void ResumeApplication() => ApplicationResuming?.Invoke();

    public bool StartCoroutineCalled { get; private set; }

    public new Coroutine StartCoroutine(IEnumerator routine)
    {
        StartCoroutineCalled = true;
        return base.StartCoroutine(routine);
    }
}
