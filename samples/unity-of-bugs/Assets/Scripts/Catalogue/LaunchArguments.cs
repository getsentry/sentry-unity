using System;
using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// Reads named launch arguments in a player: an intent extra on Android (<c>am start -e sample errors/assert</c>),
    /// a process argument on iOS and desktop (<c>--sample errors/assert</c>). Returns nothing in the Editor, so
    /// entering play mode never runs a sample by itself.
    /// </summary>
    public static class LaunchArguments
    {
        /// <summary>The ID of the sample to run at startup.</summary>
        public const string Sample = "sample";

        /// <summary>An ID that tags everything the run sends, so it can be found in Sentry.</summary>
        public const string RunId = "run_id";

        public static string Get(string name)
        {
            try
            {
                var value = Read(name);
                return string.IsNullOrEmpty(value) ? null : value;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read the launch argument '{name}': {e.Message}");
                return null;
            }
        }

#if UNITY_EDITOR
        private static string Read(string name) => null;
#elif UNITY_ANDROID
        private static string Read(string name)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
            {
                return intent.Call<string>("getStringExtra", name);
            }
        }
#elif UNITY_IOS
        // .NET's Environment.GetCommandLineArgs() doesn't see the process arguments on iOS
        private static string Read(string name) => NativeBridge.getLaunchArgument(name);
#else
        private static string Read(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i < args.Length - 1; i++)
            {
                if (args[i] == "--" + name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }
#endif
    }
}
