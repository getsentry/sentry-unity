using Sentry;
using Sentry.Unity;
using UnityEngine;

namespace Assets.Scripts
{
    public sealed class ManualSentryBehaviour : MonoBehaviour
    {
        private void OnEnable()
        {
            Debug.Log(nameof(ManualSentryBehaviour));

            SentryUnity.Init(options =>
            {
                options.Enabled = true;
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
            });
            Debug.LogError($"{nameof(ManualSentryBehaviour)} error 2.");
        }
    }
}
