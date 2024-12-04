using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal interface ISentryJava
{
    public bool IsEnabled(IJniExecutor jniExecutor);
    public bool? Init(IJniExecutor jniExecutor, SentryUnityOptions options);
    public string? GetInstallationId(IJniExecutor jniExecutor);
    public bool? CrashedLastRun(IJniExecutor jniExecutor);
    public void Close(IJniExecutor jniExecutor);
    public void WriteScope(
        IJniExecutor jniExecutor,
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
    private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

    public bool IsEnabled(IJniExecutor jniExecutor)
    {
        return jniExecutor.Run(() =>
        {
            using var sentry = GetSentryJava();
            return sentry.CallStatic<bool>("isEnabled");
        });
    }

    public bool? Init(IJniExecutor jniExecutor, SentryUnityOptions options)
    {
        jniExecutor.Run(() =>
        {
            using var sentry = new AndroidJavaClass("io.sentry.android.core.SentryAndroid");
            using var context = new AndroidJavaClass("com.unity3d.player.UnityPlayer")
                .GetStatic<AndroidJavaObject>("currentActivity");

            sentry.CallStatic("init", context, new OptionsConfiguration(androidOptions =>
            {
                androidOptions.Call("setDsn", options.Dsn);
                androidOptions.Call("setDebug", options.Debug);
                androidOptions.Call("setRelease", options.Release);
                androidOptions.Call("setEnvironment", options.Environment);

                var sentryLevelClass = new AndroidJavaClass("io.sentry.SentryLevel");
                var levelString = GetLevelString(options.DiagnosticLevel);
                var sentryLevel = sentryLevelClass.GetStatic<AndroidJavaObject>(levelString);
                androidOptions.Call("setDiagnosticLevel", sentryLevel);

                // if (options.SampleRate.HasValue)
                // {
                //     androidOptions.SetIfNotNull("setSampleRate", options.SampleRate.Value);
                // }

                androidOptions.Call("setMaxBreadcrumbs", options.MaxBreadcrumbs);
                androidOptions.Call("setMaxCacheItems", options.MaxCacheItems);

                // Causes `FormatException`. Works with `new object[] { false }`
                // androidOptions.SetIfNotNull("setSendDefaultPii", options.SendDefaultPii);
                // Note: doesn't work - produces a blank (white) screenshot
                // androidOptions.SetIfNotNull("setAttachScreenshot", false);
                // androidOptions.SetIfNotNull("setNdkIntegrationEnabled", options.NdkIntegrationEnabled);
                // androidOptions.SetIfNotNull("setNdkScopeSyncEnabled", options.NdkScopeSyncEnabled);
                // androidOptions.SetIfNotNull("setEnableAutoSessionTracking", false);
                // androidOptions.SetIfNotNull("setEnableAutoAppLifecycleBreadcrumbs", false);
                // androidOptions.SetIfNotNull("setEnableAnr", false);
                // androidOptions.SetIfNotNull("setEnablePersistentScopeObserver", false);
            }, options.DiagnosticLogger));
        });

        return IsEnabled(jniExecutor);
    }

    // Update the callback class to match the Java interface name
    internal class OptionsConfiguration : AndroidJavaProxy
    {
        private readonly Action<AndroidJavaObject> _callback;
        private readonly IDiagnosticLogger? _logger;

        public OptionsConfiguration(Action<AndroidJavaObject> callback, IDiagnosticLogger? logger)
            : base("io.sentry.Sentry$OptionsConfiguration")
        {
            _callback = callback;
            _logger = logger;
        }

        public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
        {
            try
            {
                if (methodName != "configure" || args.Length != 1)
                {
                    throw new Exception($"Invalid invocation: {methodName}({args.Length} args)");
                }
                _callback(args[0]);
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error in SentryJava.OptionsConfiguration: {0}");
            }
            return null;
        }
    }

    public string? GetInstallationId(IJniExecutor jniExecutor)
    {
        return jniExecutor.Run(() =>
        {
            using var sentry = GetSentryJava();
            using var hub = sentry.CallStatic<AndroidJavaObject>("getCurrentHub");
            using var options = hub?.Call<AndroidJavaObject>("getOptions");
            return options?.Call<string>("getDistinctId");
        });
    }

    /// <summary>
    /// Returns whether or not the last run resulted in a crash.
    /// </summary>
    /// <remarks>
    /// This value is returned by the Android SDK and reports for both ART and NDK.
    /// </remarks>
    /// <returns>
    /// True if the last run terminated in a crash. No otherwise.
    /// If the SDK wasn't able to find this information, null is returned.
    /// </returns>
    public bool? CrashedLastRun(IJniExecutor jniExecutor)
    {
        return jniExecutor.Run(() =>
        {
            using var sentry = GetSentryJava();
            using var jo = sentry.CallStatic<AndroidJavaObject>("isCrashedLastRun");
            return jo?.Call<bool>("booleanValue");
        });
    }

    public void Close(IJniExecutor jniExecutor)
    {
        jniExecutor.Run(() =>
        {
            using var sentry = GetSentryJava();
            sentry.CallStatic("close");
        });
    }

    public void WriteScope(
        IJniExecutor jniExecutor,
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
        jniExecutor.Run(() =>
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

    // Implements the io.sentry.ScopeCallback interface.
    internal class ScopeCallback : AndroidJavaProxy
    {
        private readonly Action<AndroidJavaObject> _callback;

        public ScopeCallback(Action<AndroidJavaObject> callback) : base("io.sentry.ScopeCallback")
        {
            _callback = callback;
        }

        // Note: defining the method should be enough with the default Invoke(), but in reality it doesn't work:
        // No such proxy method: Sentry.Unity.Android.SentryJava+ScopeCallback.run(UnityEngine.AndroidJavaObject)
        //   public void run(AndroidJavaObject scope) => UnityEngine.Debug.Log("run() invoked");
        // Therefore, we're overriding the Invoke() instead:
        public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
        {
            try
            {
                if (methodName != "run" || args.Length != 1)
                {
                    throw new Exception($"Invalid invocation: {methodName}({args.Length} args)");
                }
                _callback(args[0]);
            }
            catch (Exception e)
            {
                // Adding the Sentry logger tag ensures we don't send this error to Sentry.
                Debug.unityLogger.Log(LogType.Error, UnityLogger.LogTag, $"Error in SentryJava.ScopeCallback: {e}");
            }
            return null;
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
