using UnityEngine;

namespace Sentry.Unity.Android;

/// <summary>
/// Extension methods to Breadcrumb.
/// </summary>
public static class BreadcrumbExtensions
{
    /// <summary>
    /// To Java SentryLevel.
    /// </summary>
    /// <param name="level">The Breadcrumb level to convert to Java level.</param>
    /// <returns>An Android Java object representing the SentryLevel.</returns>
    public static AndroidJavaObject ToJavaSentryLevel(this BreadcrumbLevel level)
    {
        using var javaSentryLevel = new AndroidJavaClass("io.sentry.SentryLevel");
        return level switch
        {
            BreadcrumbLevel.Fatal => javaSentryLevel.GetStatic<AndroidJavaObject>("FATAL"),
            BreadcrumbLevel.Error => javaSentryLevel.GetStatic<AndroidJavaObject>("ERROR"),
            BreadcrumbLevel.Warning => javaSentryLevel.GetStatic<AndroidJavaObject>("WARNING"),
            BreadcrumbLevel.Debug => javaSentryLevel.GetStatic<AndroidJavaObject>("DEBUG"),
            // BreadcrumbLevel.Info or unknown:
            _ => javaSentryLevel.GetStatic<AndroidJavaObject>("INFO")
        };
    }
}
