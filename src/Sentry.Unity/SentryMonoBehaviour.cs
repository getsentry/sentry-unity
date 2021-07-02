using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// Singleton and DontDestroyOnLoad setup.
    /// </summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal partial class SentryMonoBehaviour
    {
        private static SentryMonoBehaviour? Instance;

        // ReSharper disable once UnusedMember.Local
        private void Awake()
        {
            // Unity overrides `==` operator in MonoBehaviours
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
            if (Instance is null)
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
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
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
        // ReSharper disable once UnusedMember.Local
        private void OnApplicationQuit() => Destroy(gameObject);
    }

    /// <summary>
    /// Main thread data collector.
    /// </summary>
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal partial class SentryMonoBehaviour
    {
        internal readonly MainThreadData MainThreadData = new();

        // ReSharper disable once UnusedMember.Local
        private void Start()
            => StartCoroutine(CollectData());

        private IEnumerator CollectData()
        {
            MainThreadData.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            yield return null;
            MainThreadData.ProcessorCount = SystemInfo.processorCount;
            MainThreadData.DeviceType = SystemInfo.deviceType.ToString();
            MainThreadData.OperatingSystem = SystemInfo.operatingSystem;
            MainThreadData.CpuDescription = SystemInfo.processorType;
            MainThreadData.SupportsVibration = SystemInfo.supportsVibration;
            MainThreadData.DeviceName = SystemInfo.deviceName;
            MainThreadData.DeviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier;
            MainThreadData.DeviceModel = SystemInfo.deviceModel;
            MainThreadData.SystemMemorySize = SystemInfo.systemMemorySize;
            yield return null;
            MainThreadData.GraphicsDeviceId = SystemInfo.graphicsDeviceID;
            MainThreadData.GraphicsDeviceName = SystemInfo.graphicsDeviceName;
            MainThreadData.GraphicsDeviceVendorId = SystemInfo.graphicsDeviceVendorID.ToString();
            MainThreadData.GraphicsDeviceVendor = SystemInfo.graphicsDeviceVendor;
            MainThreadData.GraphicsMemorySize = SystemInfo.graphicsMemorySize;
            MainThreadData.GraphicsMultiThreaded = SystemInfo.graphicsMultiThreaded;
            MainThreadData.NpotSupport = SystemInfo.npotSupport.ToString();
            MainThreadData.GraphicsDeviceVersion = SystemInfo.graphicsDeviceVersion;
            MainThreadData.GraphicsDeviceType = SystemInfo.graphicsDeviceType.ToString();
            MainThreadData.MaxTextureSize = SystemInfo.maxTextureSize;
            MainThreadData.SupportsDrawCallInstancing = SystemInfo.supportsInstancing;
            MainThreadData.SupportsRayTracing = SystemInfo.supportsRayTracing;
            MainThreadData.SupportsComputeShaders = SystemInfo.supportsComputeShaders;
            MainThreadData.SupportsGeometryShaders = SystemInfo.supportsGeometryShaders;
            MainThreadData.GraphicsShaderLevel = SystemInfo.graphicsShaderLevel;
        }
    }
}
