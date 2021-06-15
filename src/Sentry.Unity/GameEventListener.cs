using System;
using UnityEngine;

namespace Sentry.Unity
{
    public class GameEventListener : MonoBehaviour
    {
        public event Action? ApplicationEnter;
        public event Action? ApplicationExit;

        private void OnApplicationPause(bool pauseStatus)
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                return;
            }

            if (pauseStatus)
            {
                ApplicationExit?.Invoke();
            }
            else
            {
                ApplicationEnter?.Invoke();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {

            if (Application.platform != RuntimePlatform.Android)
            {
                return;
            }

            if (hasFocus)
            {
                ApplicationEnter?.Invoke();
            }
            else
            {
                ApplicationExit?.Invoke();
            }
        }
    }
}
