using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal interface ISentryJava
{
    public bool? IsEnabled(TimeSpan timeout);
    public void Init(SentryUnityOptions options, TimeSpan timeout);
    public string? GetInstallationId();
    public bool? CrashedLastRun();
    public void Close();
    public void WriteScope(
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
    public void AddBreadCrumb(Breadcrumb breadcrumb);
    public void SetExtra(string key, string? value);
    public void SetTag(string key, string? value);
    public void UnsetTag(string key);
    public void SetUser(SentryUser user);
    public void UnsetUser();
    public void SetTrace(SentryId traceId, SpanId spanId);
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
    private readonly JniExecutor? _jniExecutor;
    private IDiagnosticLogger? _logger;
    private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");
    private static AndroidJavaObject GetInternalSentryJava() => new AndroidJavaClass("io.sentry.android.core.InternalSentrySdk");

    public SentryJava(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _jniExecutor = new JniExecutor(_logger);
    }

    public bool? IsEnabled(TimeSpan timeout)
    {
        return _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            return sentry.CallStatic<bool>("isEnabled");
        }, timeout);
    }

    public void Init(SentryUnityOptions options, TimeSpan timeout)
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = new AndroidJavaClass("io.sentry.android.core.SentryAndroid");
            using var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");

            sentry.CallStatic("init", context, new AndroidOptionsConfiguration(androidOptions =>
            {
                androidOptions.Call("setDsn", options.Dsn);
                androidOptions.Call("setDebug", options.Debug);
                androidOptions.Call("setRelease", options.Release);
                androidOptions.Call("setEnvironment", options.Environment);

                var sentryLevelClass = new AndroidJavaClass("io.sentry.SentryLevel");
                var levelString = GetLevelString(options.DiagnosticLevel);
                var sentryLevel = sentryLevelClass.GetStatic<AndroidJavaObject>(levelString);
                androidOptions.Call("setDiagnosticLevel", sentryLevel);

                if (options.SampleRate.HasValue)
                {
                    androidOptions.SetIfNotNull("setSampleRate", options.SampleRate.Value);
                }

                androidOptions.Call("setMaxBreadcrumbs", options.MaxBreadcrumbs);
                androidOptions.Call("setMaxCacheItems", options.MaxCacheItems);
                androidOptions.Call("setSendDefaultPii", options.SendDefaultPii);
                androidOptions.Call("setEnableNdk", options.NdkIntegrationEnabled);
                androidOptions.Call("setEnableScopeSync", options.NdkScopeSyncEnabled);

                // Options that are not to be set by the user
                // We're disabling some integrations as to not duplicate event or because the SDK relies on the .NET SDK
                // implementation of certain feature - i.e. Session Tracking

                // Note: doesn't work - produces a blank (white) screenshot
                androidOptions.Call("setAttachScreenshot", false);
                androidOptions.Call("setEnableAutoSessionTracking", false);
                androidOptions.Call("setEnableActivityLifecycleBreadcrumbs", false);
                androidOptions.Call("setAnrEnabled", false);
                androidOptions.Call("setEnableScopePersistence", false);
            }, options.DiagnosticLogger));
        }, timeout);
    }

    public string? GetInstallationId()
    {
        return _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub?.Call<AndroidJavaObject>("getOptions");
            return options?.Call<string>("getDistinctId");
        });
    }

    /// <summary>
    /// Returns whether the last run resulted in a crash.
    /// </summary>
    /// <remarks>
    /// This value is returned by the Android SDK and reports for both ART and NDK.
    /// </remarks>
    /// <returns>
    /// True if the last run terminated in a crash. No otherwise.
    /// If the SDK wasn't able to find this information, null is returned.
    /// </returns>
    public bool? CrashedLastRun()
    {
        return _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            using var jo = sentry.CallStatic<AndroidJavaObject>("isCrashedLastRun");
            return jo?.Call<bool>("booleanValue");
        });
    }

    public void Close()
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("close");
        });
    }

    public void WriteScope(
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
        _jniExecutor?.Run(() =>
        {
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
                contexts.Call("setGpu", gpu);
            }));
        });
    }

    public bool IsSentryJavaPresent()
    {
        try
        {
            _ = GetSentryJava();
        }
        catch (AndroidJavaException)
        {
            return false;
        }

        return true;
    }

    public void AddBreadCrumb(Breadcrumb breadcrumb)
    {
        _jniExecutor?.RunAsync(() =>
        {
            using var sentry = GetSentryJava();
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            using var javaLevel = breadcrumb.Level.ToJavaSentryLevel();
            javaBreadcrumb.Set("level", javaLevel);
            sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        });
    }

    public void SetExtra(string key, string? value)
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setExtra", key, value);
        });
    }

    public void SetTag(string key, string? value)
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        });
    }

    public void UnsetTag(string key)
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        });
    }

    public void SetUser(SentryUser user)
    {
        _jniExecutor?.Run(() =>
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
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setUser", null);
        });
    }

    public void SetTrace(SentryId traceId, SpanId spanId)
    {
        _jniExecutor?.Run(() =>
        {
            using var sentry = GetInternalSentryJava();
            // We have to explicitly cast to `(Double?)`
            sentry.CallStatic("setTrace", traceId.ToString(), spanId.ToString(), (Double?)null, (Double?)null);
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
