using Sentry.Extensibility;
using UnityEngine.Analytics;

namespace Sentry.Unity.WebGL;

/// <summary>
/// Configure Sentry for WebGL
/// </summary>
public static class SentryWebGL
{
    /// <summary>
    /// Configures the WebGL support.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options)
    {
        options.DiagnosticLogger?.LogDebug("Updating configuration for Unity WebGL.");

        // Note: we need to use a custom background worker which actually doesn't work in the background
        // because Unity doesn't support async (multithreading) yet. This may change in the future so let's watch
        // https://docs.unity3d.com/2019.4/Documentation/ScriptReference/PlayerSettings.WebGL-threadsSupport.html
        options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);

        // No way to recognize crashes in WebGL yet. We may be able to do so after implementing the JS support.
        // Additionally, we could recognize the situation when the unity gets stuck due to an error in JS/native:
        //    "An abnormal situation has occurred: the PlayerLoop internal function has been called recursively.
        //     Please contact Customer Support with a sample project so that we can reproduce the problem and troubleshoot it."
        // Maybe we could write a file when this error occurs and recognize it on the next start. Like unity-native.
        options.CrashedLastRun = () => false;

        // Disable async when accessing files (e.g. FileStream(useAsync: true)) because it throws on WebGL.
        options.UseAsyncFileIO = false;

        if (options.AttachScreenshot)
        {
            options.AttachScreenshot = false;
            options.DiagnosticLogger?.LogWarning("Attaching screenshots on WebGL is disabled - " +
                                                 "it currently produces blank screenshots mid-frame.");
        }

        options.DefaultUserId = SentryInstallationIdProvider.GetInstallationId(options);

        // Indicate that this platform doesn't support running background threads.
        options.MultiThreading = false;
    }
}
