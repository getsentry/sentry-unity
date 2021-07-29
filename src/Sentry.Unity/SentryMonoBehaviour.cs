using System;
using System.Collections;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Singleton and DontDestroyOnLoad setup.
    /// </summary>
    internal partial class SentryMonoBehaviour
    {
        private static SentryMonoBehaviour? Instance;

        private void Awake()
        {
            // Unity overrides `==` operator in MonoBehaviours
            if (Instance == null)
            {
                Instance = this;
                // Don't destroy when changing scenes
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    ///  A MonoBehavior used to forward application focus events to subscribers.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    internal partial class SentryMonoBehaviour : MonoBehaviour
    {
        /// <summary>
        /// Hook to receive an event when the application gains focus.
        /// <remarks>
        /// Listens to OnApplicationFocus for all platforms except Android, where we listen to OnApplicationPause.
        /// </remarks>
        /// </summary>
        public event Action? ApplicationResuming;

        /// <summary>
        /// Hook to receive an event when the application loses focus.
        /// <remarks>
        /// Listens to OnApplicationFocus for all platforms except Android, where we listen to OnApplicationPause.
        /// </remarks>
        /// </summary>
        public event Action? ApplicationPausing;

        // Keeping internal track of running state because OnApplicationPause and OnApplicationFocus get called during startup and would fire false resume events
        private bool _isRunning = true;

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
        /// To receive Leaving/Resuming events on Android.
        /// <remarks>
        /// On Android, when the on-screen keyboard is enabled, it causes a OnApplicationFocus(false) event.
        /// Additionally, if you press "Home" at the moment the keyboard is enabled, the OnApplicationFocus() event is
        /// not called, but OnApplicationPause() is called instead.
        /// </remarks>
        /// <seealso href="https://docs.unity3d.com/2019.4/Documentation/ScriptReference/MonoBehaviour.OnApplicationPause.html"/>
        /// </summary>
        internal void OnApplicationPause(bool pauseStatus)
        {
            if (Application.Platform != RuntimePlatform.Android)
            {
                return;
            }

            if (pauseStatus && _isRunning)
            {
                _isRunning = false;
                ApplicationPausing?.Invoke();
            }
            else if (!pauseStatus && !_isRunning)
            {
                _isRunning = true;
                ApplicationResuming?.Invoke();
            }
        }

        /// <summary>
        /// To receive Leaving/Resuming events on all platforms except Android.
        /// </summary>
        /// <param name="hasFocus"></param>
        internal void OnApplicationFocus(bool hasFocus)
        {
            // To avoid event duplication on Android since the pause event will be handled via OnApplicationPause
            if (Application.Platform == RuntimePlatform.Android)
            {
                return;
            }

            if (hasFocus && !_isRunning)
            {
                _isRunning = true;
                ApplicationResuming?.Invoke();
            }
            else if (!hasFocus && _isRunning)
            {
                _isRunning = false;
                ApplicationPausing?.Invoke();
            }
        }

        // The GameObject has to destroy itself since it was created with HideFlags.HideAndDontSave
        private void OnApplicationQuit() => Destroy(gameObject);
    }

    /// <summary>
    /// Main thread data collector.
    /// </summary>
    internal partial class SentryMonoBehaviour
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

        private void Start()
            => StartCoroutine(CollectData());

        internal IEnumerator CollectData()
        {
            MainThreadData.MainThreadId = SentrySystemInfo.MainThreadId;
            yield return null;
            MainThreadData.ProcessorCount = SentrySystemInfo.ProcessorCount;
            MainThreadData.DeviceType = SentrySystemInfo.DeviceType;
            MainThreadData.OperatingSystem = SentrySystemInfo.OperatingSystem;
            MainThreadData.CpuDescription = SentrySystemInfo.CpuDescription;
            MainThreadData.SupportsVibration = SentrySystemInfo.SupportsVibration;
            MainThreadData.DeviceName = SentrySystemInfo.DeviceName;
            MainThreadData.DeviceUniqueIdentifier = SentrySystemInfo.DeviceUniqueIdentifier;
            MainThreadData.DeviceModel = SentrySystemInfo.DeviceModel;
            MainThreadData.SystemMemorySize = SentrySystemInfo.SystemMemorySize;
            yield return null;
            MainThreadData.GraphicsDeviceId = SentrySystemInfo.GraphicsDeviceId;
            MainThreadData.GraphicsDeviceName = SentrySystemInfo.GraphicsDeviceName;
            MainThreadData.GraphicsDeviceVendorId = SentrySystemInfo.GraphicsDeviceVendorId;
            MainThreadData.GraphicsDeviceVendor = SentrySystemInfo.GraphicsDeviceVendor;
            MainThreadData.GraphicsMemorySize = SentrySystemInfo.GraphicsMemorySize;
            MainThreadData.GraphicsMultiThreaded = SentrySystemInfo.GraphicsMultiThreaded;
            MainThreadData.NpotSupport = SentrySystemInfo.NpotSupport;
            MainThreadData.GraphicsDeviceVersion = SentrySystemInfo.GraphicsDeviceVersion;
            MainThreadData.GraphicsDeviceType = SentrySystemInfo.GraphicsDeviceType;
            MainThreadData.MaxTextureSize = SentrySystemInfo.MaxTextureSize;
            MainThreadData.SupportsDrawCallInstancing = SentrySystemInfo.SupportsDrawCallInstancing;
            MainThreadData.SupportsRayTracing = SentrySystemInfo.SupportsRayTracing;
            MainThreadData.SupportsComputeShaders = SentrySystemInfo.SupportsComputeShaders;
            MainThreadData.SupportsGeometryShaders = SentrySystemInfo.SupportsGeometryShaders;
            MainThreadData.GraphicsShaderLevel = SentrySystemInfo.GraphicsShaderLevel;
            MainThreadData.IsDebugBuild = SentrySystemInfo.IsDebugBuild;
            MainThreadData.InstallMode = SentrySystemInfo.InstallMode;
            MainThreadData.TargetFrameRate = SentrySystemInfo.TargetFrameRate;
            MainThreadData.CopyTextureSupport = SentrySystemInfo.CopyTextureSupport;
            MainThreadData.RenderingThreadingMode = SentrySystemInfo.RenderingThreadingMode;
        }
    }
}
