using System;
using System.Collections;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Singleton and DontDestroyOnLoad setup.
    /// </summary>
    [AddComponentMenu("")] // Hides it from being added as a component in the inspector
    public partial class SentryMonoBehaviour : MonoBehaviour
    {
        private static SentryMonoBehaviour? _instance;
        public static SentryMonoBehaviour Instance
        {
            get
            {
                // Unity overrides `==` operator in MonoBehaviours
                if (_instance == null)
                {
                    // HideAndDontSave excludes the gameObject from the scene meaning it does not get destroyed on loading/unloading
                    var sentryGameObject = new GameObject("SentryMonoBehaviour") { hideFlags = HideFlags.HideAndDontSave };
                    _instance = sentryGameObject.AddComponent<SentryMonoBehaviour>();
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// A MonoBehaviour used to provide access to helper methods used during Performance Auto Instrumentation
    /// </summary>
    public partial class SentryMonoBehaviour
    {
        public void StartAwakeSpan(MonoBehaviour monoBehaviour) =>
            SentrySdk.GetSpan()?.StartChild("awake", $"{monoBehaviour.gameObject.name}.{monoBehaviour.GetType().Name}");

        public void FinishAwakeSpan() => SentrySdk.GetSpan()?.Finish(SpanStatus.Ok);
    }

    /// <summary>
    ///  A MonoBehavior used to forward application focus events to subscribers.
    /// </summary>
    public partial class SentryMonoBehaviour
    {
        /// <summary>
        /// Hook to receive an event when the application gains focus.
        /// </summary>
        public event Action? ApplicationResuming;

        /// <summary>
        /// Hook to receive an event when the application loses focus.
        /// </summary>
        public event Action? ApplicationPausing;

        // Keeping internal track of running state because OnApplicationPause and OnApplicationFocus get called during startup and would fire false resume events
        internal bool _isRunning = true;

        private IApplication? _application;
        internal IApplication Application
        {
            get
            {
                _application ??= ApplicationAdapter.Instance;
                return _application;
            }
            set => _application = value;
        }

        /// <summary>
        /// Updates the SDK's internal pause status
        /// </summary>
        public void UpdatePauseStatus(bool paused)
        {
            if (paused && _isRunning)
            {
                _isRunning = false;
                ApplicationPausing?.Invoke();
            }
            else if (!paused && !_isRunning)
            {
                _isRunning = true;
                ApplicationResuming?.Invoke();
            }
        }

        /// <summary>
        /// To receive Pause events.
        /// </summary>
        internal void OnApplicationPause(bool pauseStatus) => UpdatePauseStatus(pauseStatus);

        /// <summary>
        /// To receive Focus events.
        /// </summary>
        /// <param name="hasFocus"></param>
        internal void OnApplicationFocus(bool hasFocus) => UpdatePauseStatus(!hasFocus);

        // The GameObject has to destroy itself since it was created with HideFlags.HideAndDontSave
        private void OnApplicationQuit() => Destroy(gameObject);
    }

    /// <summary>
    /// Main thread data collector.
    /// </summary>
    public partial class SentryMonoBehaviour
    {
        internal readonly MainThreadData MainThreadData = new();

        private ISentrySystemInfo? _sentrySystemInfo;
        internal ISentrySystemInfo SentrySystemInfo
        {
            get
            {
                _sentrySystemInfo ??= SentrySystemInfoAdapter.Instance;
                return _sentrySystemInfo;
            }
            set => _sentrySystemInfo = value;
        }

        // Note: Awake is called only once and synchronously while the object is built.
        // We want to do it this way instead of a StartCoroutine() so that we have the context info ASAP.
        private void Awake() => CollectData();

        internal void CollectData()
        {
            // Note: Awake() runs on the main thread. The following code just reads a couple of variables so there's no
            // delay on the UI and we're safe to do it on the main thread.
            MainThreadData.MainThreadId = SentrySystemInfo.MainThreadId;
            MainThreadData.ProcessorCount = SentrySystemInfo.ProcessorCount;
            MainThreadData.OperatingSystem = SentrySystemInfo.OperatingSystem;
            MainThreadData.CpuDescription = SentrySystemInfo.CpuDescription;
            MainThreadData.SupportsVibration = SentrySystemInfo.SupportsVibration;
            MainThreadData.DeviceName = SentrySystemInfo.DeviceName;
            MainThreadData.SystemMemorySize = SentrySystemInfo.SystemMemorySize;
            MainThreadData.GraphicsDeviceId = SentrySystemInfo.GraphicsDeviceId;
            MainThreadData.GraphicsDeviceName = SentrySystemInfo.GraphicsDeviceName;
            MainThreadData.GraphicsDeviceVendor = SentrySystemInfo.GraphicsDeviceVendor;
            MainThreadData.GraphicsMemorySize = SentrySystemInfo.GraphicsMemorySize;
            MainThreadData.NpotSupport = SentrySystemInfo.NpotSupport;
            MainThreadData.GraphicsDeviceVersion = SentrySystemInfo.GraphicsDeviceVersion;
            MainThreadData.GraphicsDeviceType = SentrySystemInfo.GraphicsDeviceType;
            MainThreadData.MaxTextureSize = SentrySystemInfo.MaxTextureSize;
            MainThreadData.SupportsDrawCallInstancing = SentrySystemInfo.SupportsDrawCallInstancing;
            MainThreadData.SupportsRayTracing = SentrySystemInfo.SupportsRayTracing;
            MainThreadData.SupportsComputeShaders = SentrySystemInfo.SupportsComputeShaders;
            MainThreadData.SupportsGeometryShaders = SentrySystemInfo.SupportsGeometryShaders;
            MainThreadData.GraphicsShaderLevel = SentrySystemInfo.GraphicsShaderLevel;
            MainThreadData.EditorVersion = SentrySystemInfo.EditorVersion;
            MainThreadData.InstallMode = SentrySystemInfo.InstallMode;
            if (MainThreadData.IsMainThread())
            {
                MainThreadData.DeviceType = SentrySystemInfo.DeviceType?.Value;
                MainThreadData.DeviceUniqueIdentifier = SentrySystemInfo.DeviceUniqueIdentifier?.Value;
                MainThreadData.DeviceModel = SentrySystemInfo.DeviceModel?.Value;
                MainThreadData.GraphicsDeviceVendorId = SentrySystemInfo.GraphicsDeviceVendorId?.Value;
                MainThreadData.GraphicsMultiThreaded = SentrySystemInfo.GraphicsMultiThreaded?.Value;
                MainThreadData.IsDebugBuild = SentrySystemInfo.IsDebugBuild?.Value;
                MainThreadData.TargetFrameRate = SentrySystemInfo.TargetFrameRate?.Value;
                MainThreadData.CopyTextureSupport = SentrySystemInfo.CopyTextureSupport?.Value;
                MainThreadData.RenderingThreadingMode = SentrySystemInfo.RenderingThreadingMode?.Value;
                MainThreadData.StartTime = SentrySystemInfo.StartTime?.Value;
            }
            else
            {
                // Note: while this shouldn't ever occur, we want to make sure there are some values instead of UB.
                MainThreadData.DeviceType = null;
                MainThreadData.DeviceUniqueIdentifier = null;
                MainThreadData.DeviceModel = null;
                MainThreadData.GraphicsDeviceVendorId = null;
                MainThreadData.GraphicsMultiThreaded = null;
                MainThreadData.IsDebugBuild = null;
                MainThreadData.TargetFrameRate = null;
                MainThreadData.CopyTextureSupport = null;
                MainThreadData.RenderingThreadingMode = null;
                MainThreadData.StartTime = null;
            }
        }
    }
}
