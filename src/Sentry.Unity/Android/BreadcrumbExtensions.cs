using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Extension methods to Breadcrumb.
    /// </summary>
    public static class BreadcrumbExtensions
    {
        private static readonly AndroidJavaObject JavaSentryLevel = new AndroidJavaClass("io.sentry.SentryLevel");
        private static AndroidJavaObject JavaSentryLevelFatal = JavaSentryLevel.GetStatic<AndroidJavaObject>("FATAL");
        private static AndroidJavaObject JavaSentryLevelDebug = JavaSentryLevel.GetStatic<AndroidJavaObject>("DEBUG");
        private static AndroidJavaObject JavaSentryLevelInfo = JavaSentryLevel.GetStatic<AndroidJavaObject>("INFO");
        private static AndroidJavaObject JavaSentryLevelWarning = JavaSentryLevel.GetStatic<AndroidJavaObject>("WARNING");
        private static AndroidJavaObject JavaSentryLevelError = JavaSentryLevel.GetStatic<AndroidJavaObject>("ERROR");

        /// <summary>
        /// To Java SentryLevel.
        /// </summary>
        /// <param name="level">The Breadcrumb level to convert to Java level.</param>
        /// <returns>An Android Java object representing the SentryLevel.</returns>
        public static AndroidJavaObject ToJavaSentryLevel(this BreadcrumbLevel level)
            => level switch
            {
                BreadcrumbLevel.Critical => JavaSentryLevelFatal,
                BreadcrumbLevel.Debug => JavaSentryLevelDebug,
                BreadcrumbLevel.Info => JavaSentryLevelInfo,
                BreadcrumbLevel.Warning => JavaSentryLevelWarning,
                BreadcrumbLevel.Error => JavaSentryLevelError,
                _ => JavaSentryLevelInfo
            };
    }
}
