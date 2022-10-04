using Sentry.Unity;
using UnityEngine;

public class SentryAutoBehaviour : MonoBehaviour
{
    private void Awake()
    {
        // SentrySdk.GetSpan()?.StartChild("Awake", $"{gameObject.name}.{name}");
        SentryAwakeIntegration.StartSpan(this);

        Debug.Log("Testing Awake");
        
        if (enabled)
        {
            Debug.Log("HUEHUE");

            SentryAwakeIntegration.FinishSpan();
            return;
        }
        else if (Time.deltaTime > 2.0f)
        {
            Debug.Log("sad noises");

            SentryAwakeIntegration.FinishSpan();
            return;
        }
        else
        {
            Debug.Log("End of if");
        }

        Debug.Log("End of testing Awake");

        SentryAwakeIntegration.FinishSpan();
        // SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    }
}
