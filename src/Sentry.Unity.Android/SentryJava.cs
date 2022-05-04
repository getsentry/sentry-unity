using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// JNI access to `sentry-java` methods.
    /// </summary>
    /// <remarks>
    /// The `sentry-java` SDK on Android is brought in through the `sentry-android-core`
    /// and `sentry-java` maven packages.
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    internal static class SentryJava
    {
        internal static string? GetInstallationId()
        {
            if (!Attach())
            {
                return null;
            }

            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub?.Call<AndroidJavaObject>("getOptions");
            return options?.Call<string>("getDistinctId");
        }

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
            if (!Attach())
            {
                return null;
            }
            using var sentry = GetSentryJava();
            using var jo = sentry.CallStatic<AndroidJavaObject>("isCrashedLastRun");
            return jo?.Call<bool>("booleanValue");
        }

        public static void Close()
        {
            if (Attach())
            {
                using var sentry = GetSentryJava();
                sentry.CallStatic("close");
            }
        }

        private static bool Attach() => AndroidJNI.AttachCurrentThread() == 0;
        private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");
    }
}
