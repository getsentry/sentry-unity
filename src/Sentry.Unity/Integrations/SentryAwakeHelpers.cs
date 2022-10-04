using UnityEngine;

namespace Sentry.Unity
{
    public static class SentryAwakeHelpers
    {
        public static void StartSpan(MonoBehaviour monoBehaviour)
        {
            SentrySdk.GetSpan()?.StartChild("Awake", $"{monoBehaviour.GetType().FullName}.{monoBehaviour.gameObject.name}");
        }

        public static void FinishSpan()
        {
            SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
        }
    }
}
