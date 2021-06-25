using System;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    ///  A MonoBehavior used to forward application focus events to subscribers.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    internal class ApplicationPauseListener : MonoBehaviour
    {
        /// <summary>
        /// Hook to receive an event when the application gains focus.
        /// <remarks>
        /// Listens to OnApplicationFocus for all platforms except Android, where we listen to OnApplicationPause.
        /// </remarks>
        /// </summary>
        public event Action? ApplicationResuming;

        /// <summary>
        /// Hook to receive an event when the application loses focus.
        /// <remarks>
        /// Listens to OnApplicationFocus for all platforms except Android, where we listen to OnApplicationPause.
        /// </remarks>
        /// </summary>
        public event Action? ApplicationPausing;

        // Keeping internal track of running state because OnApplicationPause and OnApplicationFocus get called during startup and would fire false resume events
        private bool _isRunning = true;

        /// <summary>
        /// To receive Leaving/Resuming events on Android.
        /// <remarks>
        /// https://docs.unity3d.com/2019.4/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html
        /// On Android, when the on-screen keyboard is enabled, it causes a OnApplicationFocus(false) event.
        /// Additionally, if you press "Home" at the moment the keyboard is enabled, the OnApplicationFocus() event is
        /// not called, but OnApplicationPause() is called instead.
        /// </remarks>
        /// </summary>
        private void OnApplicationPause(bool pauseStatus)
        {
            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            if (pauseStatus && _isRunning)
            {
                _isRunning = false;
                ApplicationPausing?.Invoke();
            }
            else if (!pauseStatus && !_isRunning)
            {
                _isRunning = true;
                ApplicationResuming?.Invoke();
            }
        }

        /// <summary>
        /// To receive Leaving/Resuming events on all platforms except Android.
        /// </summary>
        /// <param name="hasFocus"></param>
        private void OnApplicationFocus(bool hasFocus)
        {
            // To avoid event duplication on Android since the pause event will be handled via OnApplicationPause
            if (Application.platform == RuntimePlatform.Android)
            {
                return;
            }

            if (hasFocus && !_isRunning)
            {
                _isRunning = true;
                ApplicationResuming?.Invoke();
            }
            else if (!hasFocus && _isRunning)
            {
                _isRunning = false;
                ApplicationPausing?.Invoke();
            }
        }

        // The GameObject has to destroy itself since it was created with HideFlags.HideAndDontSave
        private void OnApplicationQuit()
        {
            Destroy(gameObject);
        }
    }
}
