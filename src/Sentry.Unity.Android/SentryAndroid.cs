using Sentry;
using UnityEngine;
using System;
using Sentry.Unity;

namespace Sentry.Unity.Android
{
    public class SentryAndroid : MonoBehaviour
    {

        public void Disable()
        {

        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            SentryInitialization.Init();
            InitAndroid();
        }

        private static void InitAndroid()
        {
            var sentryAndroid = new AndroidJavaClass("io.sentry.unity.SentryAndroid");
            if (sentryAndroid == null)
            {
                Debug.LogWarning("Sentry Android SDK not found.");
                return;
            }
            try
            {
                sentryAndroid.CallStatic("testThrow");
            }
            finally
            {
                sentryAndroid.Dispose();
            }
        }
    }

}
