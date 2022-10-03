using UnityEngine;

namespace Sentry.Unity
{
    public static class SentryAwakeIntegration
    {
        public static void StartSpan(MonoBehaviour monoBehaviour)
        {
            SentrySdk.GetSpan()?.StartChild("Awake", $"{monoBehaviour.gameObject.name}.{monoBehaviour.name}");
        }

        public static void FinishSpan()
        {
            SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
        }
    }
}
