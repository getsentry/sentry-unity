using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal interface ISentryJava
{
    public bool? IsEnabled();
    public bool? IsMainProcess();
    public void Init(SentryUnityOptions options);
    public string? GetOption(string getter);
    public bool? IsNdkAppHangTrackingEnabled();
    public string? GetTraceHeader();
    public string? GetBaggageHeader();
    public string? GetMainProcessTraceHeader();
    public SentryUser? GetUser();
    public bool TryGetMainProcessUser(out SentryUser? user);
    public string? GetInstallationId();
    public bool? CrashedLastRun();
    public void Close();
    public void WriteScope(
        string? AppStartTime,
        string? AppBuildType,
        int? GpuId,
        string? GpuName,
        string? GpuVendorName,
        int? GpuMemorySize,
        string? GpuNpotSupport,
        string? GpuVersion,
        string? GpuApiType,
        int? GpuMaxTextureSize,
        bool? GpuSupportsDrawCallInstancing,
        bool? GpuSupportsRayTracing,
        bool? GpuSupportsComputeShaders,
        bool? GpuSupportsGeometryShaders,
        string? GpuVendorId,
        bool? GpuMultiThreadedRendering,
        string? GpuGraphicsShaderLevel);
    public bool IsSentryJavaPresent();

    // Methods for the ScopeObserver
    public void AddBreadcrumb(Breadcrumb breadcrumb);
    public void SetExtra(string key, string? value);
    public void SetTag(string key, string? value);
    public void UnsetTag(string key);
    public void SetUser(SentryUser user);
    public void UnsetUser();
    public void SetTrace(SentryId traceId, SpanId spanId);
    public void SetEnvironment(string? environment);
    void AddAttachment(string path, string fileName, string? contentType);
    void AddAttachmentBytes(byte[] data, string fileName, string? contentType);
    void ClearAttachments();
}

/// <summary>
/// JNI access to `sentry-java` methods.
/// </summary>
/// <remarks>
/// The `sentry-java` SDK on Android is brought in through the `sentry-android-core`
/// and `sentry-java` maven packages.
/// </remarks>
/// <see href="https://github.com/getsentry/sentry-java"/>
internal class SentryJava : ISentryJava
{
    private readonly IAndroidJNI _androidJNI;
    private readonly IDiagnosticLogger? _logger;

    private readonly CancellationTokenSource _scopeSyncShutdownSource;
    private readonly AutoResetEvent _scopeSyncEvent;
    private readonly Thread _scopeSyncThread;
    private readonly ConcurrentQueue<(Action action, string actionName)> _scopeSyncItems = new();
    private static readonly TimeSpan JniResultTimeout = TimeSpan.FromMilliseconds(500);
    private volatile bool _closed;

    private static AndroidJavaObject GetInternalSentryJava() => new AndroidJavaClass("io.sentry.android.core.InternalSentrySdk");
    private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

    private static AndroidJavaObject GetCurrentActivity()
    {
        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        return unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
    }

    public SentryJava(IDiagnosticLogger? logger, IAndroidJNI? androidJNI = null)
    {
        _logger = logger;
        _androidJNI ??= androidJNI ?? AndroidJNIAdapter.Instance;

        _scopeSyncEvent = new AutoResetEvent(false);
        _scopeSyncShutdownSource = new CancellationTokenSource();
        _scopeSyncThread = new Thread(SyncScope) { IsBackground = true, Name = "SentryScopeSyncWorkerThread" };
        _scopeSyncThread.Start();
    }

    public bool? IsEnabled()
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling IsEnabled() on Android SDK requires running on MainThread");
            return null;
        }

        try
        {
            using var sentry = GetSentryJava();
            return sentry.CallStatic<bool>("isEnabled");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.IsEnabled' failed.");
        }

        return null;
    }

    // Whether the game runs in the app's main process. With Unity as a Library, the game can run in its own process.
    public bool? IsMainProcess()
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling IsMainProcess() on Android SDK requires running on MainThread");
            return null;
        }

        try
        {
            if (GetProcessName() is not { } processName)
            {
                return null;
            }

            using var activity = GetCurrentActivity();
            return processName == activity.Call<string>("getPackageName");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.IsMainProcess' failed.");
        }

        return null;
    }

    // `Application.getProcessName` requires API 28. Below, the process name is the NUL-terminated `/proc/self/cmdline`.
    private static string? GetProcessName()
    {
        using var version = new AndroidJavaClass("android.os.Build$VERSION");
        if (version.GetStatic<int>("SDK_INT") < 28)
        {
#pragma warning disable SN0001
            var cmdline = File.ReadAllText("/proc/self/cmdline");
#pragma warning restore SN0001
            var end = cmdline.IndexOf('\0');
            return end < 0 ? cmdline : cmdline.Substring(0, end);
        }

        using var application = new AndroidJavaClass("android.app.Application");
        return application.CallStatic<string>("getProcessName");
    }

    public void Init(SentryUnityOptions options)
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling Init() on Android SDK requires running on MainThread");
            return;
        }

        try
        {
            using var sentry = new AndroidJavaClass("io.sentry.android.core.SentryAndroid");
            using var context = GetCurrentActivity();

            sentry.CallStatic("init", context, new AndroidOptionsConfiguration(androidOptions =>
            {
                androidOptions.Call("setDsn", options.Dsn);
                androidOptions.Call("setDebug", options.Debug);

                // With external initialization, the native SDK keeps the app's release and environment unless they were set.
                var isExternal = options.AndroidNativeInitializationType is NativeInitializationType.External;
                if (!isExternal || options.ReleaseOverridden || options.Release != options.DefaultRelease)
                {
                    androidOptions.Call("setRelease", options.Release);
                }
                androidOptions.Call("setDist", options.Distribution);
                if (!isExternal || options.EnvironmentOverridden || options.Environment != options.DefaultEnvironment)
                {
                    androidOptions.Call("setEnvironment", options.Environment);
                }

                using var sentryLevelClass = new AndroidJavaClass("io.sentry.SentryLevel");
                var levelString = GetLevelString(options.DiagnosticLevel);
                using var sentryLevel = sentryLevelClass.GetStatic<AndroidJavaObject>(levelString);
                androidOptions.Call("setDiagnosticLevel", sentryLevel);

                if (options.SampleRate.HasValue)
                {
                    using var sampleRate = new AndroidJavaObject("java.lang.Double", (double)options.SampleRate.Value);
                    androidOptions.Call("setSampleRate", sampleRate);
                }

                androidOptions.Call("setMaxBreadcrumbs", options.MaxBreadcrumbs);
                androidOptions.Call("setMaxCacheItems", options.MaxCacheItems);
                androidOptions.Call("setSendDefaultPii", options.SendDefaultPii);
                androidOptions.Call("setEnableNdk", options.NdkIntegrationEnabled);
                androidOptions.Call("setEnableScopeSync", options.NdkScopeSyncEnabled);
                androidOptions.Call("setNativeSdkName", "sentry.native.android.unity");

                androidOptions.Call("setAnrEnabled", options.AndroidNativeAnrEnabled);
                androidOptions.Call("setEnableScopePersistence", options.AndroidNativeAnrEnabled);
                androidOptions.Call("setReportHistoricalAnrs", options.AndroidReportHistoricalAnrs);
                androidOptions.Call("setAttachAnrThreadDump", options.AndroidAttachAnrThreadDump);
                androidOptions.Call("setEnableNdkAppHangTracking", options.NativeAppHangTrackingEnabled);
                androidOptions.Call("setNdkAppHangTimeoutIntervalMillis", (long)Math.Max(0, options.AppHangTimeout.TotalMilliseconds));

                androidOptions.Call("setTombstoneEnabled", options.AndroidTombstoneEnabled);
                androidOptions.Call("setReportHistoricalTombstones", options.AndroidReportHistoricalTombstones);

                // With external initialization, the game runs in its own process, next to the app's SDK in the main
                // process. Its own cache directory keeps it from taking over the app's session and persisted scope.
                if (isExternal && GetProcessName() is { } processName)
                {
                    // Its events belong to the app's release and environment, persisted by the app's SDK.
                    if (!options.ReleaseOverridden && options.Release == options.DefaultRelease && ReadMainProcessOption(androidOptions, "release.json") is { } release)
                    {
                        androidOptions.Call("setRelease", release);
                        if (options.Distribution is null)
                        {
                            androidOptions.Call("setDist", ReadMainProcessOption(androidOptions, "dist.json"));
                        }
                    }
                    if (!options.EnvironmentOverridden && options.Environment == options.DefaultEnvironment && ReadMainProcessOption(androidOptions, "environment.json") is { } environment)
                    {
                        androidOptions.Call("setEnvironment", environment);
                    }

                    using var cacheDir = context.Call<AndroidJavaObject>("getCacheDir");
                    var processSuffix = processName.Substring(processName.IndexOf(':') + 1);
                    androidOptions.Call("setCacheDirPath", $"{cacheDir.Call<string>("getAbsolutePath")}/sentry-{processSuffix}");
                    // The game continues the app's trace instead of starting new ones on its own screens and taps.
                    androidOptions.Call("setEnableAutoTraceIdGeneration", false);
                    // From API 30, the app's SDK reports the exits of all of the app's processes, including this one.
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    if (version.GetStatic<int>("SDK_INT") >= 30)
                    {
                        androidOptions.Call("setAnrEnabled", false);
                        androidOptions.Call("setTombstoneEnabled", false);
                    }
                }

                using (var logsOptions = androidOptions.Call<AndroidJavaObject>("getLogs"))
                {
                    logsOptions.Call("setEnabled", options.EnableLogs);
                }

                // Options that are not to be set by the user
                // We're disabling some integrations as to not duplicate event or because the SDK relies on the .NET SDK
                // implementation of certain feature - i.e. Session Tracking

                // Note: doesn't work - produces a blank (white) screenshot
                androidOptions.Call("setAttachScreenshot", false);
                androidOptions.Call("setEnableAutoSessionTracking", false);
                androidOptions.Call("setEnableActivityLifecycleBreadcrumbs", false);
                // Disable user interaction tracking to prevent conflicts with VR platforms (e.g., Oculus InputHooks)
                androidOptions.Call("setEnableUserInteractionBreadcrumbs", false);
                androidOptions.Call("setEnableUserInteractionTracing", false);
            }, options.DiagnosticLogger));
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.Init' failed.");
        }
    }

    // A string option of the native SDK, e.g. `getRelease` or `getEnvironment`.
    public string? GetOption(string getter) =>
        ReadOptions(nameof(GetOption), options => options.Call<string>(getter));

    // Whether the app's native SDK runs app hang detection. It only watches a thread that sends heartbeats.
    public bool? IsNdkAppHangTrackingEnabled() =>
        ReadOptions(nameof(IsNdkAppHangTrackingEnabled), options =>
            (bool?)(options.Call<bool>("isEnableNdk") && options.Call<bool>("isEnableNdkAppHangTracking")));

    private T? ReadOptions<T>(string caller, Func<AndroidJavaObject, T?> read)
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling {0}() on Android SDK requires running on MainThread", caller);
            return default;
        }

        try
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub?.Call<AndroidJavaObject>("getOptions");
            return options is null ? default : read(options);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.{0}' failed.", caller);
        }

        return default;
    }

    // The `sentry-trace` header of the app's SDK in the main process: its active span, otherwise its propagation context.
    public string? GetMainProcessTraceHeader()
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling GetMainProcessTraceHeader() on Android SDK requires running on MainThread");
            return null;
        }

        try
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub.Call<AndroidJavaObject>("getOptions");
            using var spanContext = ReadMainProcessFile(options, ".scope-cache/trace.json", "io.sentry.SpanContext");
            if (spanContext is null)
            {
                return null;
            }

            using var traceId = spanContext.Call<AndroidJavaObject>("getTraceId");
            using var spanId = spanContext.Call<AndroidJavaObject>("getSpanId");
            using var sampled = spanContext.Call<AndroidJavaObject>("getSampled");
            var header = $"{traceId.Call<string>("toString")}-{spanId.Call<string>("toString")}";
            return sampled is null ? header : $"{header}-{(sampled.Call<bool>("booleanValue") ? 1 : 0)}";
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.GetMainProcessTraceHeader' failed.");
        }

        return null;
    }

    // The user the app's SDK in the main process persisted, null when it has none. False when the file can't be read,
    // e.g. while the app's SDK rewrites it.
    public bool TryGetMainProcessUser(out SentryUser? user)
    {
        user = null;
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling TryGetMainProcessUser() on Android SDK requires running on MainThread");
            return false;
        }

        try
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub.Call<AndroidJavaObject>("getOptions");
            using var javaUser = ReadMainProcessFile(options, ".scope-cache/user.json", "io.sentry.protocol.User", out var failed);
            if (failed)
            {
                return false;
            }

            user = ToSentryUser(javaUser);
            return true;
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.TryGetMainProcessUser' failed.");
        }

        return false;
    }

    private static SentryUser? ToSentryUser(AndroidJavaObject? user) => user is null
        ? null
        : new SentryUser
        {
            Id = user.Call<string>("getId"),
            Username = user.Call<string>("getUsername"),
            Email = user.Call<string>("getEmail"),
            IpAddress = user.Call<string>("getIpAddress")
        };

    private string? ReadMainProcessOption(AndroidJavaObject options, string fileName)
    {
        using var value = ReadMainProcessFile(options, $".options-cache/{fileName}", "java.lang.String");
        return value?.Call<string>("toString");
    }

    // A file that the app's SDK in the main process persisted to its cache directory, next to ours: same DSN hash,
    // default 'sentry' directory.
    private AndroidJavaObject? ReadMainProcessFile(AndroidJavaObject options, string path, string className) =>
        ReadMainProcessFile(options, path, className, out _);

    // Failed when the file is there but can't be read, e.g. empty or partly written.
    private AndroidJavaObject? ReadMainProcessFile(AndroidJavaObject options, string path, string className, out bool failed)
    {
        failed = false;
        try
        {
            using var cacheDir = new AndroidJavaObject("java.io.File", options.Call<string>("getCacheDirPath"));
            using var activity = GetCurrentActivity();
            using var appCacheDir = activity.Call<AndroidJavaObject>("getCacheDir");
            var filePath = $"{appCacheDir.Call<string>("getAbsolutePath")}/sentry/{cacheDir.Call<string>("getName")}/{path}";
            using var file = new AndroidJavaObject("java.io.File", filePath);
            if (!file.Call<bool>("exists"))
            {
                _logger?.LogDebug("Nothing persisted by the main process at '{0}'.", filePath);
                return null;
            }

            using var fileReader = new AndroidJavaObject("java.io.FileReader", file);
            using var reader = new AndroidJavaObject("java.io.BufferedReader", fileReader);
            try
            {
                using var serializer = options.Call<AndroidJavaObject>("getSerializer");
                using var javaClass = new AndroidJavaClass("java.lang.Class");
                using var type = javaClass.CallStatic<AndroidJavaObject>("forName", className);
                var value = serializer.Call<AndroidJavaObject>("deserialize", reader, type);
                failed = value is null;
                return value;
            }
            finally
            {
                reader.Call("close");
            }
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Reading '{0}' persisted by the main process failed.", path);
        }

        failed = true;
        return null;
    }

    public string? GetTraceHeader() => RunJniSafeAndWait(() =>
    {
        using var sentry = GetSentryJava();
        using var header = sentry.CallStatic<AndroidJavaObject>("getTraceparent");
        return header?.Call<string>("getValue");
    });

    // The user on the native SDK's scope.
    public SentryUser? GetUser() => RunJniSafeAndWait(() =>
    {
        using var sentry = GetInternalSentryJava();
        using var scope = sentry.CallStatic<AndroidJavaObject>("getCurrentScope");
        using var user = scope?.Call<AndroidJavaObject>("getUser");
        return ToSentryUser(user);
    });

    public string? GetBaggageHeader() => RunJniSafeAndWait(() =>
    {
        using var sentry = GetSentryJava();
        using var header = sentry.CallStatic<AndroidJavaObject>("getBaggage");
        return header?.Call<string>("getValue");
    });

    public string? GetInstallationId() => GetOption("getDistinctId");

    /// <summary>
    /// Returns whether the last run resulted in a crash.
    /// </summary>
    /// <remarks>
    /// This value is returned by the Android SDK and reports for both ART and NDK.
    /// </remarks>
    /// <returns>
    /// True if the last run terminated in a crash, false otherwise.
    /// If the SDK wasn't able to find this information, null is returned.
    /// </returns>
    public bool? CrashedLastRun()
    {
        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling CrashedLastRun() on Android SDK requires running on MainThread");
            return null;
        }

        try
        {
            using var sentry = GetSentryJava();
            using var jo = sentry.CallStatic<AndroidJavaObject>("isCrashedLastRun");
            return jo?.Call<bool>("booleanValue");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.CrashedLastRun' failed.");
        }

        return null;
    }

    public void WriteScope(
        string? AppStartTime,
        string? AppBuildType,
        int? GpuId,
        string? GpuName,
        string? GpuVendorName,
        int? GpuMemorySize,
        string? GpuNpotSupport,
        string? GpuVersion,
        string? GpuApiType,
        int? GpuMaxTextureSize,
        bool? GpuSupportsDrawCallInstancing,
        bool? GpuSupportsRayTracing,
        bool? GpuSupportsComputeShaders,
        bool? GpuSupportsGeometryShaders,
        string? GpuVendorId,
        bool? GpuMultiThreadedRendering,
        string? GpuGraphicsShaderLevel)
    {
        RunJniSafe(() =>
        {
            using var app = new AndroidJavaObject("io.sentry.protocol.App");
            if (AppStartTime is not null)
            {
                var epochMs = DateTimeOffset.Parse(AppStartTime).ToUnixTimeMilliseconds();
                using var date = new AndroidJavaObject("java.util.Date", epochMs);
                app.Set("appStartTime", date);
            }
            app.SetIfNotNull("buildType", AppBuildType);

            using var gpu = new AndroidJavaObject("io.sentry.protocol.Gpu");
            gpu.SetIfNotNull("name", GpuName);
            gpu.SetIfNotNull("id", GpuId);
            gpu.SetIfNotNull("vendorId", GpuVendorId);
            gpu.SetIfNotNull("vendorName", GpuVendorName);
            gpu.SetIfNotNull("memorySize", GpuMemorySize);
            gpu.SetIfNotNull("apiType", GpuApiType);
            gpu.SetIfNotNull("multiThreadedRendering", GpuMultiThreadedRendering);
            gpu.SetIfNotNull("version", GpuVersion);
            gpu.SetIfNotNull("npotSupport", GpuNpotSupport);
            using var sentry = GetSentryJava();
            sentry.CallStatic("configureScope", new ScopeCallback(scope =>
            {
                using var contexts = scope.Call<AndroidJavaObject>("getContexts");
                contexts.Call("setApp", app);
                contexts.Call("setGpu", gpu);
            }));
        });
    }

    public bool IsSentryJavaPresent()
    {
        try
        {
            using var _ = GetSentryJava();
        }
        catch (AndroidJavaException)
        {
            return false;
        }

        return true;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            using var javaLevel = breadcrumb.Level.ToJavaSentryLevel();
            javaBreadcrumb.Set("level", javaLevel);
            if (breadcrumb.Data is { Count: > 0 })
            {
                foreach (var kvp in breadcrumb.Data)
                {
                    javaBreadcrumb.Call("setData", kvp.Key, kvp.Value);
                }
            }
            sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        });
    }

    public void SetExtra(string key, string? value)
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setExtra", key, value);
        });
    }

    public void SetTag(string key, string? value)
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        });
    }

    public void UnsetTag(string key)
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        });
    }

    public void SetUser(SentryUser user)
    {
        RunJniSafe(() =>
        {
            AndroidJavaObject? javaUser = null;

            try
            {
                javaUser = new AndroidJavaObject("io.sentry.protocol.User");
                javaUser.Set("email", user.Email);
                javaUser.Set("id", user.Id);
                javaUser.Set("username", user.Username);
                javaUser.Set("ipAddress", user.IpAddress);
                using var sentry = GetSentryJava();
                sentry.CallStatic("setUser", javaUser);
            }
            finally
            {
                javaUser?.Dispose();
            }
        });
    }

    public void UnsetUser()
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setUser", null);
        });
    }

    public void SetTrace(SentryId traceId, SpanId spanId)
    {
        RunJniSafe(() =>
        {
            using var sentry = GetInternalSentryJava();
            // We have to explicitly cast to `(Double?)`
            sentry.CallStatic("setTrace", traceId.ToString(), spanId.ToString(), (Double?)null, (Double?)null);
        });
    }

    public void SetEnvironment(string? environment)
    {
        // TODO: sentry-java lacks this API
        // RunJniSafe(() =>
        // {
        //     using var sentry = GetSentryJava();
        //     sentry.CallStatic("configureScope", new ScopeCallback(scope =>
        //     {
        //         scope.Call("setEnvironment", environment);
        //     }));
        // });
    }

    public void AddAttachment(string path, string fileName, string? contentType)
    {
        RunJniSafe(() =>
        {
            using var attachment = contentType is not null
                ? new AndroidJavaObject("io.sentry.Attachment", path, fileName, contentType)
                : new AndroidJavaObject("io.sentry.Attachment", path, fileName);

            using var sentry = GetSentryJava();
            sentry.CallStatic("configureScope", new ScopeCallback(scope =>
                scope.Call("addAttachment", attachment)));
        });
    }

    public void AddAttachmentBytes(byte[] data, string fileName, string? contentType)
    {
        RunJniSafe(() =>
        {
            using var attachment = contentType is not null
                ? new AndroidJavaObject("io.sentry.Attachment", data, fileName, contentType)
                : new AndroidJavaObject("io.sentry.Attachment", data, fileName);

            using var sentry = GetSentryJava();
            sentry.CallStatic("configureScope", new ScopeCallback(scope =>
                scope.Call("addAttachment", attachment)));
        });
    }

    public void ClearAttachments()
    {
        RunJniSafe(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("configureScope", new ScopeCallback(scope =>
                scope.Call("clearAttachments")));
        });
    }

    // https://github.com/getsentry/sentry-java/blob/db4dfc92f202b1cefc48d019fdabe24d487db923/sentry/src/main/java/io/sentry/SentryLevel.java#L4-L9
    internal static string GetLevelString(SentryLevel level) => level switch
    {
        SentryLevel.Debug => "DEBUG",
        SentryLevel.Error => "ERROR",
        SentryLevel.Fatal => "FATAL",
        SentryLevel.Info => "INFO",
        SentryLevel.Warning => "WARNING",
        _ => "DEBUG"
    };

    internal void RunJniSafe(Action action, [CallerMemberName] string actionName = "", bool? isMainThread = null)
    {
        if (_closed)
        {
            _logger?.LogInfo("Scope sync is closed, skipping '{0}'", actionName);
            return;
        }

        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is true)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Calling '{0}' failed.", actionName);
            }
        }
        else
        {
            _scopeSyncItems.Enqueue((action, actionName));
            _scopeSyncEvent.Set();
        }
    }

    // Like `RunJniSafe`, but waits for the result. Off the main thread, the call runs on the scope sync thread, which
    // is attached to the JVM.
    internal T? RunJniSafeAndWait<T>(Func<T?> func, [CallerMemberName] string actionName = "", bool? isMainThread = null)
    {
        if (_closed)
        {
            return default;
        }

        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is true || Thread.CurrentThread == _scopeSyncThread)
        {
            try
            {
                return func();
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Calling '{0}' failed.", actionName);
                return default;
            }
        }

        T? result = default;
        var done = new ManualResetEventSlim(false);
        _scopeSyncItems.Enqueue((() =>
        {
            try
            {
                result = func();
            }
            finally
            {
                done.Set();
            }
        }, actionName));
        _scopeSyncEvent.Set();

        if (!done.Wait(JniResultTimeout))
        {
            _logger?.LogWarning("Timed out waiting for '{0}'.", actionName);
        }

        return result;
    }

    private void SyncScope()
    {
        _androidJNI.AttachCurrentThread();

        try
        {
            var waitHandles = new[] { _scopeSyncEvent, _scopeSyncShutdownSource.Token.WaitHandle };

            while (true)
            {
                var index = WaitHandle.WaitAny(waitHandles);
                if (index > 0)
                {
                    // Shutdown requested
                    break;
                }

                while (_scopeSyncItems.TryDequeue(out var workItem))
                {
                    var (action, actionName) = workItem;
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        _logger?.LogError(e, "Calling '{0}' failed.", actionName);
                    }
                }
            }
        }
        finally
        {
            _androidJNI.DetachCurrentThread();
        }
    }

    public void Close()
    {
        if (_closed)
        {
            return;
        }

        _closed = true;

        _scopeSyncShutdownSource.Cancel();
        _scopeSyncThread.Join();

        // Note: We intentionally don't dispose _scopeSyncEvent to avoid race conditions
        // where other threads might call RunJniSafe() after Close() but before disposal.
        // The memory overhead of a single AutoResetEvent is negligible.
        _scopeSyncShutdownSource.Dispose();

        if (!MainThreadData.IsMainThread())
        {
            _logger?.LogError("Calling Close() on Android SDK requires running on MainThread. " +
                "Scope sync thread stopped but Java SDK was not closed.");
            return;
        }

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("close");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.Close' failed.");
        }
    }
}

internal static class AndroidJavaObjectExtension
{
    public static void SetIfNotNull<T>(this AndroidJavaObject javaObject, string property, T? value, string? valueClass = null)
    {
        if (value is not null)
        {
            if (valueClass is null)
            {
                javaObject.Set(property, value!);
            }
            else
            {
                using var valueObject = new AndroidJavaObject(valueClass, value!);
                javaObject.Set(property, valueObject);
            }
        }
    }
    public static void SetIfNotNull(this AndroidJavaObject javaObject, string property, int? value) =>
        SetIfNotNull(javaObject, property, value, "java.lang.Integer");
    public static void SetIfNotNull(this AndroidJavaObject javaObject, string property, bool value) =>
        SetIfNotNull(javaObject, property, value, "java.lang.Boolean");
    public static void SetIfNotNull(this AndroidJavaObject javaObject, string property, bool? value) =>
        SetIfNotNull(javaObject, property, value, "java.lang.Boolean");
}
