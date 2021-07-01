using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;
using Sentry.Unity.Integrations;
using UnityEngine;
using DeviceOrientation = Sentry.Protocol.DeviceOrientation;
using OperatingSystem = Sentry.Protocol.OperatingSystem;

namespace Sentry.Unity
{
    internal static class UnitySdkInfo
    {
        public static string Version { get; } = typeof(UnitySdkInfo).Assembly.GetNameAndVersion().Version ?? "0.0.0";

        public const string Name = "sentry.dotnet.unity";
        public const string PackageName = "upm:sentry.unity";
    }

    internal class UnityEventProcessor : ISentryEventProcessor
    {
        private readonly SentryOptions _sentryOptions;
        private readonly MainThreadData _mainThreadData;
        private readonly IApplication _application;

        public UnityEventProcessor(SentryOptions sentryOptions, IApplication? application = null, Func<SentryMonoBehaviour>? sentryMonoBehaviourGenerator = null)
        {
            _sentryOptions = sentryOptions;
            _mainThreadData = sentryMonoBehaviourGenerator?.Invoke().MainThreadData ?? new MainThreadData { MainThreadId = 1 }; // test
            _application = application ?? ApplicationAdapter.Instance;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            try
            {
                PopulateSdk(@event.Sdk);
                PopulateApp(@event.Contexts.App);
                PopulateOperatingSystem(@event.Contexts.OperatingSystem);
                PopulateDevice(@event.Contexts.Device);
                PopulateGpu(@event.Contexts.Gpu);
                PopulateUnity((Protocol.Unity)@event.Contexts.GetOrAdd(Protocol.Unity.Type, _ => new Protocol.Unity()));
                PopulateTags(@event);
            }
            catch (Exception ex)
            {
                _sentryOptions.DiagnosticLogger?.LogError("{0} processing failed.", ex, nameof(SentryEvent));
            }

            @event.ServerName = null;

            return @event;
        }

        private static void PopulateSdk(SdkVersion sdk)
        {
            sdk.AddPackage(UnitySdkInfo.PackageName, UnitySdkInfo.Version);
            sdk.Name = UnitySdkInfo.Name;
            sdk.Version = UnitySdkInfo.Version;
        }

        private void PopulateApp(App app)
        {
            if (_mainThreadData.IsMainThread())
            {
                app.StartTime = DateTimeOffset.UtcNow
                    // NOTE: Time API requires main thread
                    .AddSeconds(-Time.realtimeSinceStartup);
            }

            app.BuildType = Debug.isDebugBuild ? "debug" : "release";
        }

        private void PopulateOperatingSystem(OperatingSystem operatingSystem)
        {
            // TODO: Will move to raw_description once parsing is done in Sentry
            operatingSystem.Name = _mainThreadData.OperatingSystem;
        }

        private void PopulateDevice(Device device)
        {
            device.ProcessorCount = SystemInfo.processorCount;
            device.SupportsVibration = SystemInfo.supportsVibration;
            device.BatteryStatus = SystemInfo.batteryStatus.ToString();
            device.DeviceType = SystemInfo.deviceType.ToString();
            device.CpuDescription = SystemInfo.processorType;
            device.Timezone = TimeZoneInfo.Local;
            device.ProcessorCount = SystemInfo.processorCount;
            device.SupportsVibration = SystemInfo.supportsVibration;
            device.Name = SystemInfo.deviceName;

            // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
            device.Simulator = _application.IsEditor ? true : null;
            device.DeviceUniqueIdentifier = _sentryOptions.SendDefaultPii ? SystemInfo.deviceUniqueIdentifier : null;

            var model = SystemInfo.deviceModel;
            if (model != SystemInfo.unsupportedIdentifier
                // Returned by the editor
                && model != "System Product Name (System manufacturer)")
            {
                device.Model = model;
            }

#pragma warning disable RECS0018 // Value is exact when expressing no battery level
            if (SystemInfo.batteryLevel != -1.0)
#pragma warning restore RECS0018
            {
                device.BatteryLevel = (short?)(SystemInfo.batteryLevel * 100);
            }

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            if (SystemInfo.systemMemorySize != 0)
            {
                device.MemorySize = SystemInfo.systemMemorySize * 1048576L; // Sentry device mem is in Bytes
            }

            switch (Input.deviceOrientation)
            {
                case UnityEngine.DeviceOrientation.Portrait:
                case UnityEngine.DeviceOrientation.PortraitUpsideDown:
                    device.Orientation = DeviceOrientation.Portrait;
                    break;
                case UnityEngine.DeviceOrientation.LandscapeLeft:
                case UnityEngine.DeviceOrientation.LandscapeRight:
                    device.Orientation = DeviceOrientation.Landscape;
                    break;
                case UnityEngine.DeviceOrientation.FaceUp:
                case UnityEngine.DeviceOrientation.FaceDown:
                    // TODO: Add to protocol?
                    break;
            }
        }

        private static void PopulateGpu(Gpu gpu)
        {
            gpu.Id = SystemInfo.graphicsDeviceID;
            gpu.Name = SystemInfo.graphicsDeviceName;
            gpu.VendorId = SystemInfo.graphicsDeviceVendorID.ToString();
            gpu.VendorName = SystemInfo.graphicsDeviceVendor;
            gpu.MemorySize = SystemInfo.graphicsMemorySize;
            gpu.MultiThreadedRendering = SystemInfo.graphicsMultiThreaded;
            gpu.NpotSupport = SystemInfo.npotSupport.ToString();
            gpu.Version = SystemInfo.graphicsDeviceVersion;
            gpu.ApiType = SystemInfo.graphicsDeviceType.ToString();
            gpu.MaxTextureSize = SystemInfo.maxTextureSize;
            gpu.SupportsDrawCallInstancing = SystemInfo.supportsInstancing;
            gpu.SupportsRayTracing = SystemInfo.supportsRayTracing;
            gpu.SupportsComputeShaders = SystemInfo.supportsComputeShaders;
            gpu.SupportsGeometryShaders = SystemInfo.supportsGeometryShaders;
            gpu.GraphicsShaderLevel = ToGraphicShaderLevelDescription(SystemInfo.graphicsShaderLevel);

            static string ToGraphicShaderLevelDescription(int shaderLevel)
                => shaderLevel switch
                {
                    20 => "Shader Model 2.0",
                    25 => "Shader Model 2.5",
                    30 => "Shader Model 3.0",
                    35 => "OpenGL ES 3.0",
                    40 => "Shader Model 4.0",
                    45 => "Metal / OpenGL ES 3.1",
                    46 => "OpenGL 4.1",
                    50 => "Shader Model 5.0",
                    _ => shaderLevel.ToString()
                };
        }

        private static void PopulateUnity(Protocol.Unity unity)
        {
            unity.InstallMode = Application.installMode.ToString();
        }

        private void PopulateTags(SentryEvent @event)
        {
            @event.SetTag("unity.gpu.supports_instancing", SystemInfo.supportsInstancing ? "true" : "false");
            @event.SetTag("unity.device.device_type", SystemInfo.deviceType.ToString());
            @event.SetTag("unity.install_mode", Application.installMode.ToString());

            if (_sentryOptions.SendDefaultPii)
            {
                @event.SetTag("unity.device.unique_identifier", SystemInfo.deviceUniqueIdentifier);
            }
        }
    }

    internal class UnityEventExceptionProcessor : ISentryEventExceptionProcessor
    {
        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            if (exception is UnityLogException ule)
            {
                // TODO: At this point the original (Mono+.NET stack trace factories already ran)
                // Ideally this strategy would fit into the SDK hooks, even though this parse gives not only
                // a stacktrace but also the exception message and type so currently can't be hooked into StackTraceFactory
                sentryEvent.SentryExceptions = new[] { ule.ToSentryException() };
                sentryEvent.SetTag("source", "log");
            }
        }
    }
}
