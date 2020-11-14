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

        // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            // TODO: from config
            // var dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
            // SentryInitialization.Init(dsn);
            // InitAndroid(dsn);
        }

        private static void InitAndroid(string dsn)
        {
            var sentryAndroid = new AndroidJavaClass("io.sentry.unity.SentryAndroidPlugin");
            if (sentryAndroid == null)
            {
                Debug.LogWarning("Sentry Android Plugin not found. Cannot initialize the Android SDK");
                return;
            }

            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var context = activity.Call<AndroidJavaObject>("getApplicationContext");
            sentryAndroid.CallStatic("init", context, dsn);
        }

        public static void TestThrow()
        {
            var buggy = new AndroidJavaClass("io.sentry.unity.Buggy");
            if (buggy == null)
            {
                Debug.Log("Buggy not found.");
                return;
            }
            try
            {
                buggy.CallStatic("testThrow");
            }
            finally
            {
                buggy.Dispose();
            }
        }
    }

}
