using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.iOS;

/// <summary>
/// Access to the Sentry native support on iOS/macOS.
/// </summary>
public static class SentryNativeCocoa
{
    private static IDiagnosticLogger? Logger;

    /// <summary>
    /// Configures the native support.
    /// </summary>
    /// <param name="options">The Sentry Unity options to use.</param>
    public static void Configure(SentryUnityOptions options) =>
        Configure(options, ApplicationAdapter.Instance.Platform);

    // For testing
    internal static void Configure(SentryUnityOptions options, RuntimePlatform platform)
    {
        Logger = options.DiagnosticLogger;
        Logger?.LogInfo("Attempting to configure native support via the Cocoa SDK");

        if (!options.IsNativeSupportEnabled(platform))
        {
            Logger?.LogDebug("Native support is disabled for: '{0}'", platform);
            return;
        }

        var isExternal = platform == RuntimePlatform.IPhonePlayer &&
                         options.IosNativeInitializationType is NativeInitializationType.External;

        if (platform == RuntimePlatform.IPhonePlayer)
        {
            if (SentryCocoaBridgeProxy.IsEnabled())
            {
                Logger?.LogDebug("The native SDK is already initialized");
            }
            else if (isExternal)
            {
                Logger?.LogWarning("The native SDK is expected to be initialized by the app but it is not. " +
                                   "Make sure the app initializes Sentry before the game starts. Skipping native support.");
                return;
            }
            else if (!SentryCocoaBridgeProxy.Init(options))
            {
                Logger?.LogWarning("Failed to initialize the native SDK");
                return;
            }

            options.ScopeObserver = new NativeScopeObserver("iOS", options, isExternal);

            if (isExternal)
            {
                UseAppReleaseAndEnvironment(options);
                NativePropagationContext.Use(options, SentryCocoaBridgeProxy.GetTraceHeader);
                RemoveUnityContextWhileUnloaded(options);

                if (options.AutoSessionTracking)
                {
                    Logger?.LogDebug("Disabling session tracking - the app tracks sessions.");
                    options.AutoSessionTracking = false;
                }
            }
        }
        else
        {
            if (!SentryCocoaBridgeProxy.Init(options))
            {
                Logger?.LogWarning("Failed to initialize the native SDK");
                return;
            }
            options.ScopeObserver = new NativeScopeObserver("macOS", options);
        }

        // The app's options decide whether sentry-cocoa tracks app hangs, so the C# ANR watchdog stays in charge.
        if (!isExternal && options.EnableAppHangTracking)
        {
            Logger?.LogDebug("Disabling the C# ANR watchdog - sentry-cocoa handles app hang detection.");
#pragma warning disable CS0618 // Type or member is obsolete
            options.DisableAnrIntegration();
#pragma warning restore CS0618 // Type or member is obsolete
        }

        if (!isExternal)
        {
            SentryCocoaBridgeProxy.SetSdkName(); // Since we're not building the SDK we have to overwrite the name here
        }

        options.NativeContextWriter = new NativeContextWriter();
        options.EnableScopeSync = true;
        options.CrashedLastRun = () =>
        {
            Logger?.LogDebug("Checking for 'CrashedLastRun'");

            var crashedLastRun = SentryCocoaBridgeProxy.CrashedLastRun() == 1;
            Logger?.LogDebug("Native SDK reported: 'crashedLastRun': '{0}'", crashedLastRun);

            return crashedLastRun;
        };

        // The app owns the SDK's lifetime.
        if (!isExternal)
        {
            options.NativeSupportCloseCallback += () => Close(options);
        }

        if (options.UnityInfo.IL2CPP)
        {
            var installationId = SentryCocoaBridgeProxy.GetInstallationId();
            if (!string.IsNullOrEmpty(installationId))
            {
                options.DefaultUserId = installationId;
            }
            else
            {
                Logger?.LogDebug("Failed to fetch 'Installation ID' from the native SDK.");
            }
        }

        Logger?.LogInfo("Successfully configured the native SDK");
    }

    // Events from the game belong to the app's release and environment, unless they were set in the options.
    private static void UseAppReleaseAndEnvironment(SentryUnityOptions options)
    {
        if (options.Release == options.DefaultRelease && SentryCocoaBridgeProxy.GetOption("releaseName") is { } release)
        {
            Logger?.LogDebug("Using the app's release: {0}", release);
            options.Release = release;
            options.Distribution ??= SentryCocoaBridgeProxy.GetOption("dist");
        }

        if (options.Environment == options.DefaultEnvironment && SentryCocoaBridgeProxy.GetOption("environment") is { } environment)
        {
            Logger?.LogDebug("Using the app's environment: {0}", environment);
            options.Environment = environment;
        }
    }

    // The app keeps running while the game is unloaded, and can load it again. The C# SDK stays initialized.
    private static void RemoveUnityContextWhileUnloaded(SentryUnityOptions options)
    {
        var unloaded = false;
        Application.unloading += () =>
        {
            Logger?.LogDebug("Removing the Unity context from the app's scope.");
            SentryCocoaBridgeProxy.RemoveContext("unity");
            unloaded = true;
        };
        SceneManager.sceneLoaded += (_, _) =>
        {
            if (!unloaded || options.NativeContextWriter is not { } contextWriter)
            {
                return;
            }

            unloaded = false;
            SentrySdk.ConfigureScope(contextWriter.Write);
        };
    }

    /// <summary>
    /// Closes the native Cocoa support.
    /// </summary>
    public static void Close(SentryUnityOptions options)
    {
        Logger?.LogInfo("Attempting to close the Cocoa SDK");

        if (!options.IsNativeSupportEnabled())
        {
            Logger?.LogDebug("Cocoa Native Support is not enable. Skipping closing the Cocoa SDK");
            return;
        }

        Logger?.LogDebug("Closing the Cocoa SDK");
        SentryCocoaBridgeProxy.Close();
    }
}
