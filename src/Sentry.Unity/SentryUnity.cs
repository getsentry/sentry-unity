using System;
using System.IO;
using System.ComponentModel;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity initialization class.
    /// </summary>
    public static class SentryUnity
    {
        private static FileStream? LockFile;

        /// <summary>
        /// Initializes Sentry Unity SDK while configuring the options.
        /// </summary>
        /// <param name="sentryUnityOptionsConfigure">Callback to configure the options.</param>
        public static void Init(Action<SentryUnityOptions> sentryUnityOptionsConfigure)
        {
            var options = new SentryUnityOptions();
            sentryUnityOptionsConfigure.Invoke(options);
            Init(options);
        }

        /// <summary>
        /// Initializes Sentry Unity SDK while providing an options object.
        /// </summary>
        /// <param name="options">The options object.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Init(SentryUnityOptions options)
        {
            options.SetupLogging();
            if (options.ShouldInitializeSdk())
            {
                // On Standalone, we disable cache dir in case multiple app instances run over the same path.
                // Note: we cannot use a named Mutex, because Unit doesn't support it. Instead, we create a file with `FileShare.None`.
                // https://forum.unity.com/threads/unsupported-internal-call-for-il2cpp-mutex-createmutex_internal-named-mutexes-are-not-supported.387334/
                if (ApplicationAdapter.Instance.Platform is RuntimePlatform.WindowsPlayer && options.CacheDirectoryPath is not null)
                {
                    try
                    {
                        LockFile = new FileStream(Path.Combine(options.CacheDirectoryPath, "sentry-unity.lock"), FileMode.OpenOrCreate,
                                FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (Exception ex)
                    {
                        options.DiagnosticLogger?.LogWarning("An exception was thrown while trying to " +
                            "acquire a lockfile on the config directory: .NET event cache will be disabled.", ex);
                        options.CacheDirectoryPath = null;
                        options.AutoSessionTracking = false;

                    }
                }

                var sentryDotNet = SentrySdk.Init(options);

                if (options.AttachScreenshot)
                {
                    SentrySdk.ConfigureScope(s =>
                        s.AddAttachment(new ScreenshotAttachment(
                            new ScreenshotAttachmentContent(options, SentryMonoBehaviour.Instance))));
                }

                if (options.NativeContextWriter is { } contextWriter)
                {
                    try
                    {
                        SentrySdk.ConfigureScopeAsync((scope) => Task.Run(() => contextWriter.Write(scope)));
                    }
                    catch (Exception e)
                    {
                        options.DiagnosticLogger?.LogWarning("Failed to synchronize scope to the native SDK: {0}", e);
                    }
                }

                ApplicationAdapter.Instance.Quitting += () =>
                {
                    options.DiagnosticLogger?.LogDebug("Closing the sentry-dotnet SDK");
                    try
                    {
                        sentryDotNet.Dispose();
                    }
                    finally
                    {
                        try
                        {
                            // We don't really need to close, Windows would release the lock anyway, but let's be nice.
                            LockFile?.Close();
                        }
                        catch (Exception ex)
                        {
                            options.DiagnosticLogger?.Log(SentryLevel.Warning,
                                "Exception while releasing the lockfile on the config directory.", ex);
                        }
                    }
                };
            }
        }
    }
}
