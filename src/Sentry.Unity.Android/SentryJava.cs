using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal interface ISentryJava
{
    public bool? IsEnabled();
    public void Init(SentryUnityOptions options);
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
    public void AddBreadcrumb(Breadcrumb breadcrumb);
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
    private readonly IAndroidJNI _androidJNI;
    private IDiagnosticLogger? _logger;
    private static AndroidJavaObject GetInternalSentryJava() => new AndroidJavaClass("io.sentry.android.core.InternalSentrySdk");
    protected virtual AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

    public SentryJava(IDiagnosticLogger? logger, IAndroidJNI? androidJNI = null)
    {
        _logger = logger;
        _androidJNI ??= androidJNI ?? AndroidJNIAdapter.Instance;
    }

    public bool? IsEnabled()
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            return sentry.CallStatic<bool>("isEnabled");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.IsEnabled' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }

        return null;
    }

    public void Init(SentryUnityOptions options)
    {
        HandleJniThreadAttachment();

        try
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
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.Init' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public string? GetInstallationId()
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub?.Call<AndroidJavaObject>("getOptions");
            return options?.Call<string>("getDistinctId");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.GetInstallationId' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }

        return null;
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
        HandleJniThreadAttachment();

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
        finally
        {
            HandleJniThreadDetachment();
        }

        return null;
    }

    public void Close()
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("close");
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.Close' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
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
        HandleJniThreadAttachment();

        try
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
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.WriteScope' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
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

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            using var javaLevel = breadcrumb.Level.ToJavaSentryLevel();
            javaBreadcrumb.Set("level", javaLevel);
            sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.AddBreadcrumb' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public void SetExtra(string key, string? value)
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setExtra", key, value);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.SetExtra' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public void SetTag(string key, string? value)
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.SetTag' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public void UnsetTag(string key)
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.UnsetTag' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public void SetUser(SentryUser user)
    {
        HandleJniThreadAttachment();
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
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.SetUser' failed.");
        }
        finally
        {
            javaUser?.Dispose();
            HandleJniThreadDetachment();
        }
    }

    public void UnsetUser()
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("setUser", null);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.UnsetUser' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
    }

    public void SetTrace(SentryId traceId, SpanId spanId)
    {
        HandleJniThreadAttachment();

        try
        {
            using var sentry = GetInternalSentryJava();
            // We have to explicitly cast to `(Double?)`
            sentry.CallStatic("setTrace", traceId.ToString(), spanId.ToString(), (Double?)null, (Double?)null);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Calling 'SentryJava.SetTrace' failed.");
        }
        finally
        {
            HandleJniThreadDetachment();
        }
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

    internal void HandleJniThreadAttachment(bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is false)
        {
            _androidJNI.AttachCurrentThread();
        }
    }

    internal void HandleJniThreadDetachment(bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is false)
        {
            _androidJNI.DetachCurrentThread();
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
