using System;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// JNI access to `sentry-java` methods.
    /// </summary>
    /// <remarks>
    /// The `sentry-java` SDK on Android is brought in through the `sentry-android-core`
    /// and `sentry-java` maven packages.
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    internal static class SentryJava
    {
        internal static string? GetInstallationId()
        {
            return SentryJniExecutor.Run(() =>
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
        public static bool? CrashedLastRun()
        {
            return SentryJniExecutor.Run(() =>
            {
                using var sentry = GetSentryJava();
                using var jo = sentry.CallStatic<AndroidJavaObject>("isCrashedLastRun");
                return jo?.Call<bool>("booleanValue");
            });
        }

        public static void Close()
        {
            SentryJniExecutor.Run(() =>
            {
                using var sentry = GetSentryJava();
                sentry.CallStatic("close");
            });
        }

        private static AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

        public static void WriteScope(
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
            SentryJniExecutor.Run(() =>
            {
                using var gpu = new AndroidJavaObject("io.sentry.protocol.Gpu");
                gpu.SetIfNotNull("name", GpuName);
                gpu.SetIfNotNull("id", GpuId);
                int intVendorId;
                if (GpuVendorId is not null && int.TryParse(GpuVendorId, out intVendorId) && intVendorId != 0)
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

        private static void SetIfNotNull<T>(this AndroidJavaObject javaObject, string property, T? value, string? valueClass = null)
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

        private static void SetIfNotNull(this AndroidJavaObject javaObject, string property, int? value) =>
            SetIfNotNull(javaObject, property, value, "java.lang.Integer");

        private static void SetIfNotNull(this AndroidJavaObject javaObject, string property, bool? value) =>
            SetIfNotNull(javaObject, property, value, "java.lang.Boolean");

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
}
