using System;
using System.IO;
using System.ComponentModel;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Xml;
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
        private static IDisposable? DotnetSdk;
        private static SentryUnityOptions? Options;

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
            Options = options;

            Options.SetupLogging();
            if (Options.ShouldInitializeSdk())
            {
                // On Standalone, we disable cache dir in case multiple app instances run over the same path.
                // Note: we cannot use a named Mutex, because Unit doesn't support it. Instead, we create a file with `FileShare.None`.
                // https://forum.unity.com/threads/unsupported-internal-call-for-il2cpp-mutex-createmutex_internal-named-mutexes-are-not-supported.387334/
                if (ApplicationAdapter.Instance.Platform is RuntimePlatform.WindowsPlayer && Options.CacheDirectoryPath is not null)
                {
                    try
                    {
                        LockFile = new FileStream(Path.Combine(Options.CacheDirectoryPath, "sentry-unity.lock"), FileMode.OpenOrCreate,
                                FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (Exception ex)
                    {
                        Options.DiagnosticLogger?.LogWarning("An exception was thrown while trying to " +
                                                             "acquire a lockfile on the config directory: .NET event cache will be disabled.", ex);
                        Options.CacheDirectoryPath = null;
                        Options.AutoSessionTracking = false;
                    }
                }

                DotnetSdk = SentrySdk.Init(options);

                if (Options.AttachScreenshot)
                {
                    SentrySdk.ConfigureScope(s =>
                        s.AddAttachment(new ScreenshotAttachment(
                            new ScreenshotAttachmentContent(options, SentryMonoBehaviour.Instance))));
                }

                if (Options.NativeContextWriter is { } contextWriter)
                {
                    SentrySdk.ConfigureScope((scope) =>
                    {
                        var task = Task.Run(() => contextWriter.Write(scope)).ContinueWith(t =>
                        {
                            if (t.Exception is not null)
                            {
                                Options.DiagnosticLogger?.LogWarning(
                                    "Failed to synchronize scope to the native SDK: {0}", t.Exception);
                            }
                        });
                    });
                }

                ApplicationAdapter.Instance.Quitting += Close;
            }
        }

        /// <summary>
        /// Closes the Sentry Unity SDK
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void Close()
        {
            Options?.DiagnosticLogger?.LogDebug("Closing the sentry-dotnet SDK");
            try
            {
                ApplicationAdapter.Instance.Quitting -= Close;

                if (Options is not null)
                {
                    Options.NativeSupportCloseCallback?.Invoke();
                    Options.NativeSupportCloseCallback = null;
                }

                DotnetSdk?.Dispose();
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
                    Options?.DiagnosticLogger?.Log(SentryLevel.Warning,
                        "Exception while releasing the lockfile on the config directory.", ex);
                }
            }
        }
    }
}
