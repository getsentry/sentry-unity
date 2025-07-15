using System;
using System.IO;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using Sentry.Unity.NativeUtils;
using UnityEngine;

namespace Sentry.Unity;

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

        options.SetupUnityLogging();
        if (!options.ShouldInitializeSdk())
        {
            return null;
        }

        MainThreadData.CollectData();

        // Some integrations are controlled through a flag and opt-in. Adding these integrations late so we have equal
        // behaviour whether the options got created through the ScriptableObject or the SDK gets manually initialized
        AddIntegrations(options);
        SetUpWindowsPlayerCaching(unitySdk, options);

        ConfigureUnsupportedPlatformFallbacks(options);

        unitySdk._dotnetSdk = Sentry.SentrySdk.Init(options);

        // We can safely call this during initialization. If the SDK self-initialized we're right on time. If the SDK
        // was initialized manually, the RuntimeOnLoad attributes already triggered, making this call a no-op.
        StartupTracingIntegration.StartTracing();

        if (options.NativeContextWriter is { } contextWriter)
        {
            Sentry.SentrySdk.CurrentHub.ConfigureScope((scope) =>
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

    public SentrySdk.CrashedLastRun CrashedLastRun()
    {
        if (_options.CrashedLastRun is null)
        {
            _options.DiagnosticLogger?.LogDebug("The SDK does not have a 'CrashedLastRun' set. " +
                                                "This might be due to a missing or disabled native integration.");
            return SentrySdk.CrashedLastRun.Unknown;
        }

        return _options.CrashedLastRun.Invoke()
            ? SentrySdk.CrashedLastRun.Crashed
            : SentrySdk.CrashedLastRun.DidNotCrash;
    }

    public void CaptureFeedback(string message, string? email, string? name, bool addScreenshot)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            _options.LogError("To submit a feedback, you must provide a message.");
            return;
        }

        var hint = addScreenshot
            ? SentryHint.WithAttachments(
                new SentryAttachment(
                    AttachmentType.Default,
                    new ByteAttachmentContent(SentryScreenshot.Capture(_options)),
                    "screenshot.jpg",
                    "image/jpeg"))
            : null;

        Sentry.SentrySdk.CurrentHub.CaptureFeedback(message, email, name, hint: hint);
    }

    internal static void SetUpWindowsPlayerCaching(SentryUnitySdk unitySdk, SentryUnityOptions options)
    {
        // On Windows-Standalone, we disable cache dir in case multiple app instances run over the same path.
        // Note: we cannot use a named Mutex, because Unity doesn't support it. Instead, we create a file with `FileShare.None`.
        // https://forum.unity.com/threads/unsupported-internal-call-for-il2cpp-mutex-createmutex_internal-named-mutexes-are-not-supported.387334/
        if (ApplicationAdapter.Instance.Platform is not RuntimePlatform.WindowsPlayer ||
            options.CacheDirectoryPath is null)
        {
            return;
        }

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

    internal static void AddIntegrations(SentryUnityOptions options)
    {
        if (options.AttachViewHierarchy)
        {
            options.AddEventProcessor(new ViewHierarchyEventProcessor(options));
        }
        if (options.AttachScreenshot)
        {
            options.AddEventProcessor(new ScreenshotEventProcessor(options));
        }

        if (!ApplicationAdapter.Instance.IsEditor &&
            options.UnityInfo.IL2CPP &&
            options.Il2CppLineNumberSupportEnabled)
        {
            if (options.UnityInfo.Il2CppMethods is not null)
            {
                options.AddExceptionProcessor(new UnityIl2CppEventExceptionProcessor(options));
            }
            else
            {
                options.DiagnosticLogger?.LogWarning("Failed to find required IL2CPP methods - Skipping line number support");
            }
        }
    }

    internal static void ConfigureUnsupportedPlatformFallbacks(SentryUnityOptions options)
    {
        if (!options.UnityInfo.IsKnownPlatform())
        {
            options.DisableFileWrite = true;

            // Requires file access, see https://github.com/getsentry/sentry-unity/issues/290#issuecomment-1163608988
            if (options.AutoSessionTracking)
            {
                options.DiagnosticLogger?.LogDebug("Platform support for automatic session tracking is unknown: disabling.");
                options.AutoSessionTracking = false;
            }

            // This is only provided on a best-effort basis for other than the explicitly supported platforms.
            if (options.BackgroundWorker is null)
            {
                options.DiagnosticLogger?.LogDebug("Platform support for background thread execution is unknown: using WebBackgroundWorker.");
                options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);
            }
        }
    }
}
