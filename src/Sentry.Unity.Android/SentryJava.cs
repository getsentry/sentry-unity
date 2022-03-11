using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// P/Invoke to `sentry-java` methods.
    /// </summary>
    /// <remarks>
    /// The `sentry-java` SDK on Android is brought in through the `sentry-android-core`
    /// and `sentry-java` maven packages.
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    public static class SentryJava
    {
        /// <summary>
        /// Returns whether or not the last run resulted in a crash.
        /// </summary>
        /// <remarks>
        /// This value is returned by the Android SDK and reports for both ART and NDK.
        /// </remarks>
        /// <returns>
        /// True if the last run terminated in a crash. No otherwise.
        /// If the SDK wasn't able to find this information, null is returned.
        /// </returns>
        public static bool? CrashedLastRun()
        {
            using var jo = new AndroidJavaObject("io.sentry.Sentry");
            return jo.CallStatic<AndroidJavaObject>("isCrashedLastRun")
                ?.Call<bool>("booleanValue");
        }

        public static void Close()
        {
            using var jo = new AndroidJavaObject("io.sentry.Sentry");
            jo.CallStatic("close");
        }
    }
}
