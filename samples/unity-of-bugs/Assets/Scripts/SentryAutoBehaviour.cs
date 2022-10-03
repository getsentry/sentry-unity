using Sentry.Unity;
using UnityEngine;

public class SentryAutoBehaviour : MonoBehaviour
{
    private void Awake()
    {
        // SentrySdk.GetSpan()?.StartChild("Awake", $"{gameObject.name}.{name}");
        SentryAwakeIntegration.StartSpan(this);

        Debug.Log("HUEHUE");

        SentryAwakeIntegration.FinishSpan();
        // SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    }
}
