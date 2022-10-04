using Sentry.Unity;
using UnityEngine;

public class SentryAutoBehaviour : MonoBehaviour
{
    private void Awake()
    {
        // SentrySdk.GetSpan()?.StartChild("Awake", $"{gameObject.name}.{name}");
        SentryAwakeHelpers.StartSpan(this);

        Debug.Log("Testing Awake");

        if (enabled)
        {
            Debug.Log("HUEHUE");

            SentryAwakeHelpers.FinishSpan();
            return;
        }
        else if (Time.deltaTime > 2.0f)
        {
            Debug.Log("sad noises");

            SentryAwakeHelpers.FinishSpan();
            return;
        }
        else
        {
            Debug.Log("End of if");
        }

        Debug.Log("End of testing Awake");

        SentryAwakeHelpers.FinishSpan();
        // SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    }
}
