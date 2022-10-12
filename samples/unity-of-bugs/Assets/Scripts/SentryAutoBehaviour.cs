using System;
using Sentry;
using Sentry.Unity;
using UnityEngine;

public class SentryAutoBehaviour : MonoBehaviour
{
    private void Awake()
    {
        // SentryMonoBehaviour.Instance.StartAwakeSpan(this);
        Debug.Log($"Testing simple Awake of '{typeof(SentryAutoBehaviour).FullName}'");
        // SentryMonoBehaviour.Instance.FinishAwakeSpan();
    }

    // private void Awake()
    // {
    //     SentrySdk.GetSpan()?.StartChild("Awake", $"{gameObject.name}.{name}");
    //     Debug.Log("Testing Awake");
    //
    //     if (enabled)
    //     {
    //         Debug.Log("HUEHUE");
    //
    //         SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    //         return;
    //     }
    //     else if (Time.deltaTime > 2.0f)
    //     {
    //         Debug.Log("sad noises");
    //
    //         SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    //         return;
    //     }
    //     else
    //     {
    //         Debug.Log("End of if");
    //     }
    //
    //     Debug.Log("End of testing Awake");
    //     SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    // }
}
