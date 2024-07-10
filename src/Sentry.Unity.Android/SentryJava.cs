using System;
using UnityEngine;

namespace Sentry.Unity.Android;

internal interface ISentryJava
{
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

    private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

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
            if (GpuVendorId is not null && int.TryParse(GpuVendorId, out var intVendorId) && intVendorId != 0)
            {
                using var integer = new AndroidJavaObject("java.lang.Integer", intVendorId);
                gpu.Set("vendorId", integer);
            }

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

    public static void SetIfNotNull(this AndroidJavaObject javaObject, string property, bool? value) =>
        SetIfNotNull(javaObject, property, value, "java.lang.Boolean");
}
