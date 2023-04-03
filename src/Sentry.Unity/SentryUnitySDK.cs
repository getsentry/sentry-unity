using System;
using System.IO;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    internal class SentryUnitySdk
    {
        private readonly SentryUnityOptions _options;
        private IDisposable _dotnetSdk = null!;
        private FileStream? _lockFile;

        private SentryUnitySdk(SentryUnityOptions options)
        {
            _options = options;
        }

        internal static SentryUnitySdk? Init(SentryUnityOptions options)
        {
            var unitySdk = new SentryUnitySdk(options);

            options.SetupLogging();
            if (!options.ShouldInitializeSdk())
            {
                return null;
            }

            // On Standalone, we disable cache dir in case multiple app instances run over the same path.
            // Note: we cannot use a named Mutex, because Unit doesn't support it. Instead, we create a file with `FileShare.None`.
            // https://forum.unity.com/threads/unsupported-internal-call-for-il2cpp-mutex-createmutex_internal-named-mutexes-are-not-supported.387334/
            if (ApplicationAdapter.Instance.Platform is RuntimePlatform.WindowsPlayer && options.CacheDirectoryPath is not null)
            {
                try
                {
                    unitySdk._lockFile = new FileStream(Path.Combine(options.CacheDirectoryPath, "sentry-unity.lock"), FileMode.OpenOrCreate,
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

            unitySdk._dotnetSdk = SentrySdk.Init(options);

            if (options.AttachScreenshot)
            {
                SentrySdk.ConfigureScope(s =>
                    s.AddAttachment(new ScreenshotAttachment(
                        new ScreenshotAttachmentContent(options, SentryMonoBehaviour.Instance))));
            }

            if (options.AttachViewHierarchy)
            {
                SentrySdk.ConfigureScope(s =>
                    s.AddAttachment(new ViewHierarchyAttachment(
                        new UnityViewHierarchyAttachmentContent(options, SentryMonoBehaviour.Instance))));
            }

            if (options.NativeContextWriter is { } contextWriter)
            {
                SentrySdk.ConfigureScope((scope) =>
                {
                    var task = Task.Run(() => contextWriter.Write(scope)).ContinueWith(t =>
                    {
                        if (t.Exception is not null)
                        {
                            options.DiagnosticLogger?.LogWarning(
                                "Failed to synchronize scope to the native SDK: {0}", t.Exception);
                        }
                    });
                });
            }

            ApplicationAdapter.Instance.Quitting += unitySdk.Close;

            return unitySdk;
        }

        public void Close()
        {
            _options.DiagnosticLogger?.LogDebug("Closing the sentry-dotnet SDK");
            try
            {
                ApplicationAdapter.Instance.Quitting -= Close;
                _options.NativeSupportCloseCallback?.Invoke();
                _options.NativeSupportCloseCallback = null;

                _dotnetSdk.Dispose();
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Warning,
                    "Exception while closing the .NET SDK.", ex);
            }

            try
            {
                // We don't really need to close, Windows would release the lock anyway, but let's be nice.
                _lockFile?.Close();
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Warning,
                    "Exception while releasing the lockfile on the config directory.", ex);
            }
        }
    }
}
