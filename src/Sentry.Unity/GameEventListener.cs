using System;
using UnityEngine;

namespace Sentry.Unity
{
    public class GameEventListener : MonoBehaviour
    {
        public event Action<bool>? ApplicationPause;
        public event Action<bool>? ApplicationFocus;

        private void Awake()
        {
            Debug.Log("Start listening to game events.");
        }

        // Gets initially called by Awake
        private void OnApplicationPause(bool pauseStatus)
        {
            Debug.Log($"Paused: {pauseStatus}");
            ApplicationPause?.Invoke(pauseStatus);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            Debug.Log($"Has focus: {hasFocus}");
            ApplicationFocus?.Invoke(hasFocus);
        }
    }
}
