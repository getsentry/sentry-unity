using UnityEngine;

namespace Sentry
{
    /// <summary>
    /// Behaviour that adds a view of the last error message that has been sent to Sentry.
    /// </summary>
    public class SentryBehaviour : MonoBehaviour
    {
        private const string ClearButtonLabel = "Clear";

        private void OnGUI()
        {
            if (SentryRuntime.Instance.LastErrorMessage != "")
            {
                GUILayout.TextArea(SentryRuntime.Instance.LastErrorMessage);
                if (GUILayout.Button(ClearButtonLabel))
                {
                    SentryRuntime.Instance.ClearLastErrorMessage();
                }
            }
        }
    }
}