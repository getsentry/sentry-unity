using UnityEngine;

namespace Sentry.Unity
{
    public class SentryScene : MonoBehaviour
    {
        public void AddButtonBreadcrumb(string message)
            => SentrySdk.AddBreadcrumb(message, "unity.ui.button");
    }
}
