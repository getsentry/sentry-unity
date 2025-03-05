using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.Analytics;

namespace Sentry.Unity.Android;

/// <summary>
/// Access to the Sentry native support on Android.
/// </summary>
public static class SentryNativeAndroid
{
    internal static IJniExecutor? JniExecutor;
    internal static ISentryJava SentryJava = new SentryJava();

    /// <summary>
    /// Configures the native Android support.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo)
    {
        options.DiagnosticLogger?.LogInfo("Attempting to configure native support via the Android SDK");

        if (!options.AndroidNativeSupportEnabled)
        {
            options.DiagnosticLogger?.LogDebug("Native support is disabled for Android");
            return;
        }

        options.DiagnosticLogger?.LogDebug("Checking whether the Android SDK is present.");

        if (!SentryJava.IsSentryJavaPresent())
        {
            options.DiagnosticLogger?.LogError("Android Native Support has been enabled but the " +
                                               "Android SDK is missing. This could have been caused by a mismatching" +
                                               "build time / runtime configuration. Please make sure you have " +
                                               "Android Native Support enabled during build time.");
            return;
        }

        JniExecutor ??= new JniExecutor(options.DiagnosticLogger);

        options.DiagnosticLogger?.LogDebug("Checking whether the Android SDK has already been initialized");

        if (SentryJava.IsEnabled(JniExecutor, TimeSpan.FromMilliseconds(200)))
        {
            options.DiagnosticLogger?.LogDebug("The Android SDK is already initialized");
        }
        else
        {
            options.DiagnosticLogger?.LogInfo("Initializing the Android SDK");

            // Local testing had Init at an average of about 25ms.
            SentryJava.Init(JniExecutor, options, TimeSpan.FromMilliseconds(200));

            options.DiagnosticLogger?.LogDebug("Validating Android SDK initialization");

            if (!SentryJava.IsEnabled(JniExecutor, TimeSpan.FromMilliseconds(200)))
            {
                options.DiagnosticLogger?.LogError("Failed to initialize Android Native Support");
                return;
            }
        }

        options.DiagnosticLogger?.LogDebug("Configuring scope sync");

        options.NativeContextWriter = new NativeContextWriter(JniExecutor, SentryJava);
        options.ScopeObserver = new AndroidJavaScopeObserver(options, JniExecutor);
        options.EnableScopeSync = true;
        options.CrashedLastRun = () =>
        {
            options.DiagnosticLogger?.LogDebug("Checking for `CrashedLastRun`");

            var crashedLastRun = SentryJava.CrashedLastRun(JniExecutor);
            if (crashedLastRun is null)
            {
                // Could happen if the Android SDK wasn't initialized before the .NET layer.
                options.DiagnosticLogger?
                    .LogWarning(
                        "Unclear from the native SDK if the previous run was a crash. Assuming it was not.");
                crashedLastRun = false;
            }
            else
            {
                options.DiagnosticLogger?
                    .LogDebug("Native Android SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);
            }

            return crashedLastRun.Value;
        };

        try
        {
            options.DiagnosticLogger?.LogDebug("Reinstalling native backend.");

            // At this point Unity has taken the signal handler and will not invoke the original handler (Sentry)
            // So we register our backend once more to make sure user-defined data is available in the crash report.
            SentryNative.ReinstallBackend();
        }
        catch (Exception e)
        {
            options.DiagnosticLogger?.LogError(
                e, "Failed to reinstall backend. Captured native crashes will miss scope data and tag.");
        }

        options.NativeSupportCloseCallback = () => Close(options, sentryUnityInfo);

        options.DiagnosticLogger?.LogDebug("Fetching installation ID");

        options.DefaultUserId = SentryJava.GetInstallationId(JniExecutor);
        if (string.IsNullOrEmpty(options.DefaultUserId))
        {
            // In case we can't get an installation ID we create one and sync that down to the native layer
            options.DiagnosticLogger?.LogDebug(
                "Failed to fetch 'Installation ID' from the native SDK. Creating new 'Default User ID'.");

            // We fall back to Unity's Analytics Session Info: https://docs.unity3d.com/ScriptReference/Analytics.AnalyticsSessionInfo-userId.html
            // It's a randomly generated GUID that gets created immediately after installation helping
            // to identify the same instance of the game
            options.DefaultUserId = AnalyticsSessionInfo.userId;
            if (options.DefaultUserId is not null)
            {
                options.ScopeObserver.SetUser(new SentryUser { Id = options.DefaultUserId });
            }
            else
            {
                options.DiagnosticLogger?.LogDebug("Failed to create new 'Default User ID'.");
            }
        }

        options.DiagnosticLogger?.LogInfo("Successfully configured the Android SDK");
    }

    public static void SetTraceId(SentryUnityOptions options)
    {
        options.DiagnosticLogger?.LogInfo("Setting new Trace");
        SentrySdk.ContinueTrace(SentryId.Create().ToString(), null);
    }

    /// <summary>
    /// Closes the native Android support.
    /// </summary>
    public static void Close(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo) =>
        Close(options, sentryUnityInfo, ApplicationAdapter.Instance.Platform);

    internal static void Close(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo, RuntimePlatform platform)
    {
        options.DiagnosticLogger?.LogInfo("Attempting to close the Android SDK");

        if (!sentryUnityInfo.IsNativeSupportEnabled(options, platform) || !SentryJava.IsSentryJavaPresent())
        {
            options.DiagnosticLogger?.LogDebug("Android Native Support is not enable. Skipping closing the Android SDK");
            return;
        }

        // Sentry Native is initialized and closed by the Java SDK, no need to call into it directly
        options.DiagnosticLogger?.LogDebug("Closing the Android SDK");

        // This is an edge-case where the Android SDK has been enabled and setup during build-time but is being
        // shut down at runtime. In this case Configure() has not been called and there is no JniExecutor yet
        JniExecutor ??= new JniExecutor(options.DiagnosticLogger);
        SentryJava?.Close(JniExecutor);
        JniExecutor.Dispose();
    }
}
