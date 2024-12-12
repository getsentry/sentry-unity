using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Unity.Integrations;
using Sentry.Extensibility;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity;

/// <summary>
/// Sentry Unity Options.
/// </summary>
/// <remarks>
/// Options to configure Unity while extending the Sentry .NET SDK functionality.
/// </remarks>
public sealed class SentryUnityOptions : SentryOptions
{
    /// <summary>
    /// UPM name of Sentry Unity SDK (package.json)
    /// </summary>
    public const string PackageName = "io.sentry.unity";

    /// <summary>
    /// Whether the SDK should automatically enable or not.
    /// </summary>
    /// <remarks>
    /// At a minimum, the <see cref="Dsn"/> need to be provided.
    /// </remarks>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// "Whether the SDK should automatically create traces during startup."
    /// </summary>
    public bool AutoStartupTraces { get; set; } = true;

    /// <summary>
    /// "Whether the SDK should automatically create traces when loading scenes."
    /// </summary>
    public bool AutoSceneLoadTraces { get; set; } = true;

    /// <summary>
    /// Whether Sentry events should be captured while in the Unity Editor.
    /// </summary>
    // Lower entry barrier, likely set to false after initial setup.
    public bool CaptureInEditor { get; set; } = true;

    /// <summary>
    /// Whether Sentry events should be debounced it too frequent.
    /// </summary>
    public bool EnableLogDebouncing { get; set; } = false;

    /// <summary>
    /// Timespan between sending events of LogType.Log
    /// </summary>
    public TimeSpan DebounceTimeLog { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Timespan between sending events of LogType.Warning
    /// </summary>
    public TimeSpan DebounceTimeWarning { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Timespan between sending events of LogType.Assert, LogType.Exception and LogType.Error
    /// </summary>
    public TimeSpan DebounceTimeError { get; set; } = TimeSpan.FromSeconds(1);


    private CompressionLevelWithAuto _requestBodyCompressionLevel = CompressionLevelWithAuto.Auto;

    /// <summary>
    /// The level which to compress the request body sent to Sentry.
    /// </summary>
    public new CompressionLevelWithAuto RequestBodyCompressionLevel
    {
        get => _requestBodyCompressionLevel;
        set
        {
            _requestBodyCompressionLevel = value;
            if (value == CompressionLevelWithAuto.Auto)
            {
                // TODO: If WebGL, then NoCompression, else .. optimize (e.g: adapt to platform)
                // The target platform is known when building the player, so 'auto' should resolve there(here).
                // Since some platforms don't support GZipping fallback: no compression.
                base.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
            }
            else
            {
                // Auto would result in -1 set if not treated before providing the options to the Sentry .NET SDK
                // DeflateStream would throw System.ArgumentOutOfRangeException
                base.RequestBodyCompressionLevel = (CompressionLevel)value;
            }
        }
    }

    /// <summary>
    /// Try to attach a current screen capture on error events.
    /// </summary>
    public bool AttachScreenshot { get; set; } = false;

    /// <summary>
    /// Try to attach the current scene's hierarchy.
    /// </summary>
    public bool AttachViewHierarchy { get; set; } = false;

    /// <summary>
    /// Maximum number of captured GameObjects in a scene root.
    /// </summary>
    public int MaxViewHierarchyRootObjects { get; set; } = 100;

    /// <summary>
    /// Maximum number of child objects captured for each GameObject.
    /// </summary>
    public int MaxViewHierarchyObjectChildCount { get; set; } = 20;

    /// <summary>
    /// Maximum depth of the hierarchy to capture. For example, setting 1 will only capture root GameObjects.
    /// </summary>
    public int MaxViewHierarchyDepth { get; set; } = 10;

    /// <summary>
    /// The quality of the attached screenshot
    /// </summary>
    public ScreenshotQuality ScreenshotQuality { get; set; } = ScreenshotQuality.High;

    /// <summary>
    /// The JPG compression quality of the attached screenshot
    /// </summary>
    public int ScreenshotCompression { get; set; } = 75;

    /// <summary>
    /// Whether the SDK should automatically add breadcrumbs per LogType
    /// </summary>
    public Dictionary<LogType, bool> AddBreadcrumbsForLogType { get; set; }

    /// <summary>
    /// The duration in [ms] for how long the game has to be unresponsive before an ANR event is reported.
    /// </summary>
    public TimeSpan AnrTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Whether the SDK should automatically filter `Bad Gateway Exceptions` caused by Unity.
    /// </summary>
    public bool FilterBadGatewayExceptions { get; set; } = true;

    /// <summary>
    /// Whether the SDK should add native support for iOS
    /// </summary>
    public bool IosNativeSupportEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should initialize the native SDK before the game starts. This bakes the options at build-time into
    /// the generated Xcode project. Modifying the options at runtime will not affect the options used to initialize
    /// the native SDK.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="NativeInitializationType.BuildTime"/>, the options are written and hardcoded into the
    /// Xcode project during the build process. This means that the options cannot be changed at runtime, as they are
    /// embedded into the project itself.
    /// </remarks>
    public NativeInitializationType IosNativeInitializationType { get; set; } = NativeInitializationType.Runtime;

    /// <summary>
    /// Whether the SDK should add native support for Android
    /// </summary>
    public bool AndroidNativeSupportEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should initialize the native SDK before the game starts. This bakes the options at build-time into
    /// the generated Gradle project. Modifying the options at runtime will not affect the options used to initialize
    /// the native SDK.
    /// </summary>
    /// <remarks>
    /// When set to <see cref="NativeInitializationType.BuildTime"/>, the options are written and hardcoded into the
    /// Gradle project during the build process. This means that the options cannot be changed at runtime, as they are
    /// embedded into the project itself.
    /// </remarks>
    public NativeInitializationType AndroidNativeInitializationType { get; set; } = NativeInitializationType.Runtime;

    /// <summary>
    /// Whether the SDK should add the NDK integration for Android
    /// </summary>
    public bool NdkIntegrationEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should sync the scope to the NDK layer for Android
    /// </summary>
    public bool NdkScopeSyncEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should add native support for Windows
    /// </summary>
    public bool WindowsNativeSupportEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should add native support for MacOS
    /// </summary>
    public bool MacosNativeSupportEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should add native support for Linux
    /// </summary>
    public bool LinuxNativeSupportEnabled { get; set; } = true;

    /// <summary>
    /// Whether the SDK should add IL2CPP line number support
    /// </summary>
    /// <remarks>
    /// To give line numbers, Sentry requires the debug symbols Unity generates during build
    /// For that reason, uploading debug information files must be enabled.
    /// For that, Org Slut, Project Slug and Auth token are required.
    /// </remarks>
    public bool Il2CppLineNumberSupportEnabled { get; set; } = true;

    /// <summary>
    /// Enable automatic performance transaction tracking.
    /// </summary>
    public bool PerformanceAutoInstrumentationEnabled { get; set; } = false;

    /// <summary>
    /// This option is restricted due to incompatibility between IL2CPP and Enhanced mode.
    /// </summary>
    public new StackTraceMode StackTraceMode { get; private set; }

    // Initialized by native SDK binding code to set the User.ID in .NET (UnityEventProcessor).
    internal string? _defaultUserId;
    internal string? DefaultUserId
    {
        get => _defaultUserId;
        set
        {
            _defaultUserId = value;
            if (_defaultUserId is null)
            {
                DiagnosticLogger?.LogWarning("Couldn't set the default user ID - the value is NULL.");
            }
            else
            {
                DiagnosticLogger?.LogDebug("Setting '{0}' as the default user ID.", _defaultUserId);
            }
        }
    }

    // Whether components & integrations can use multi-threading.
    internal bool MultiThreading = true;

    /// <summary>
    /// Used to synchronize context from .NET to the native SDK
    /// </summary>
    internal ContextWriter? NativeContextWriter { get; set; } = null;

    /// <summary>
    /// Used to close down the native SDK
    /// </summary>
    internal Action? NativeSupportCloseCallback { get; set; } = null;

    internal List<string> SdkIntegrationNames { get; set; } = new();

    public SentryUnityOptions() : this(false, ApplicationAdapter.Instance) { }

    internal SentryUnityOptions(bool isBuilding, IApplication application) :
        this(SentryMonoBehaviour.Instance, application, isBuilding)
    { }

    internal SentryUnityOptions(SentryMonoBehaviour behaviour, IApplication application, bool isBuilding)
    {
        // IL2CPP doesn't support Process.GetCurrentProcess().StartupTime
        DetectStartupTime = StartupTimeDetectionMode.Fast;

        this.AddInAppExclude("UnityEngine");
        this.AddInAppExclude("UnityEditor");
        var processor = new UnityEventProcessor(this);
        this.AddEventProcessor(processor);
        this.AddTransactionProcessor(processor);

        this.AddIntegration(new UnityLogHandlerIntegration(this));
        this.AddIntegration(new AnrIntegration(behaviour));
        this.AddIntegration(new UnityScopeIntegration(application));
        this.AddIntegration(new UnityBeforeSceneLoadIntegration());
        this.AddIntegration(new SceneManagerIntegration());
        this.AddIntegration(new SessionIntegration(behaviour));

        this.AddExceptionFilter(new UnityBadGatewayExceptionFilter());
        this.AddExceptionFilter(new UnityWebExceptionFilter());
        this.AddExceptionFilter(new UnitySocketExceptionFilter());

        IsGlobalModeEnabled = true;

        AutoSessionTracking = true;
        RequestBodyCompressionLevel = CompressionLevelWithAuto.NoCompression;
        InitCacheFlushTimeout = System.TimeSpan.Zero;

        // Ben.Demystifer not compatible with IL2CPP. We could allow Enhanced in the future for Mono.
        // See https://github.com/getsentry/sentry-unity/issues/675
        base.StackTraceMode = StackTraceMode.Original;
        IsEnvironmentUser = false;

        if (application.ProductName is string productName
            && !string.IsNullOrWhiteSpace(productName)
            && productName.Any(c => c != '.')) // productName consisting solely of '.'
        {
            productName = Regex.Replace(productName, @"\n|\r|\t|\/|\\|\.{2}|@", "_");
            Release = $"{productName}@{application.Version}";
        }
        else
        {
            Release = application.Version;
        }

        Environment = application.IsEditor && !isBuilding
            ? "editor"
            : "production";

        AddBreadcrumbsForLogType = new Dictionary<LogType, bool>
        {
            { LogType.Log, true},
            { LogType.Warning, true},
            { LogType.Assert, true},
            { LogType.Error, true},
            { LogType.Exception, true},
        };
    }

    public override string ToString()
    {
        return $@"Sentry SDK Options:
Capture In Editor: {CaptureInEditor}
Release: {Release}
Environment: {Environment}
Offline Caching: {(CacheDirectoryPath is null ? "disabled" : "enabled")}
";
    }
}

/// <summary>
/// <see cref="CompressionLevel"/> with an additional value for Automatic
/// </summary>
public enum CompressionLevelWithAuto
{
    /// <summary>
    /// The Unity SDK will attempt to choose the best option for the target player.
    /// </summary>
    Auto = -1,
    /// <summary>
    /// The compression operation should be optimally compressed, even if the operation takes a longer time (and CPU) to complete.
    /// Not supported on IL2CPP.
    /// </summary>
    Optimal = CompressionLevel.Optimal,
    /// <summary>
    /// The compression operation should complete as quickly as possible, even if the resulting data is not optimally compressed.
    /// Not supported on IL2CPP.
    /// </summary>
    Fastest = CompressionLevel.Fastest,
    /// <summary>
    /// No compression should be performed.
    /// </summary>
    NoCompression = CompressionLevel.NoCompression,
}

/// <summary>
/// Controls for the JPEG compression quality of the attached screenshot
/// </summary>
public enum ScreenshotQuality
{
    /// <summary>
    /// Full quality
    /// </summary>
    Full,
    /// <summary>
    /// High quality
    /// </summary>
    High,
    /// <summary>
    /// Medium quality
    /// </summary>
    Medium,
    /// <summary>
    /// Low quality
    /// </summary>
    Low
}

public enum NativeInitializationType
{
    /// <summary>
    /// The native SDK will be initialized at runtime through the C# layer. Options that you set programmatically
    /// will apply to the native SDK as well.
    /// </summary>
    Runtime,
    /// <summary>
    /// The SDK will bake the options available at build time. The native SDK will auto-initialize outside the of the
    /// game. Options that you modify programmatically will not apply to the native SDK.
    /// </summary>
    BuildTime,
}
