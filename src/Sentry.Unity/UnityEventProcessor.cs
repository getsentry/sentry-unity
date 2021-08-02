﻿using System;
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


        public UnityEventProcessor(SentryOptions sentryOptions, SentryMonoBehaviour sentryMonoBehaviour, IApplication? application = null)
        {
            _sentryOptions = sentryOptions;
            _mainThreadData = sentryMonoBehaviour.MainThreadData;
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

            var isDebugBuild = SafeLazyUnwrap(_mainThreadData.IsDebugBuild, nameof(app.BuildType));
            app.BuildType = isDebugBuild.HasValue
                ? isDebugBuild.Value
                    ? "debug"
                    : "release"
                : null;
        }

        private void PopulateOperatingSystem(OperatingSystem operatingSystem)
        {
            // TODO: Will move to raw_description once parsing is done in Sentry
            operatingSystem.Name = _mainThreadData.OperatingSystem;
        }

        private void PopulateDevice(Device device)
        {
            device.ProcessorCount = _mainThreadData.ProcessorCount;
            device.CpuDescription = _mainThreadData.CpuDescription;
            device.Timezone = TimeZoneInfo.Local;
            device.SupportsVibration = _mainThreadData.SupportsVibration;
            device.Name = _mainThreadData.DeviceName;

            // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
            device.Simulator = _application.IsEditor ? true : null;
            device.DeviceUniqueIdentifier = _sentryOptions.SendDefaultPii
                ? SafeLazyUnwrap(_mainThreadData.DeviceUniqueIdentifier, nameof(device.DeviceUniqueIdentifier))
                : null;
            device.DeviceType = SafeLazyUnwrap(_mainThreadData.DeviceType, nameof(device.DeviceType));

            var model = SafeLazyUnwrap(_mainThreadData.DeviceModel, nameof(device.Model));
            if (model != SystemInfo.unsupportedIdentifier
                // Returned by the editor
                && model != "System Product Name (System manufacturer)")
            {
                device.Model = model;
            }

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            if (_mainThreadData.SystemMemorySize > 0)
            {
                device.MemorySize = _mainThreadData.SystemMemorySize * 1048576L; // Sentry device mem is in Bytes
            }

            if (_mainThreadData.IsMainThread())
            {
                device.BatteryStatus = SystemInfo.batteryStatus.ToString(); // don't cache

                var batteryLevel = SystemInfo.batteryLevel;
#pragma warning disable RECS0018 // Value is exact when expressing no battery level
                if (batteryLevel != -1.0)
#pragma warning restore RECS0018
                {
                    device.BatteryLevel = (short?)(batteryLevel * 100); // don't cache
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
        }

        private void PopulateGpu(Gpu gpu)
        {
            gpu.Id = _mainThreadData.GraphicsDeviceId;
            gpu.Name = _mainThreadData.GraphicsDeviceName;
            gpu.VendorName = _mainThreadData.GraphicsDeviceVendor;
            gpu.MemorySize = _mainThreadData.GraphicsMemorySize;
            gpu.NpotSupport = _mainThreadData.NpotSupport;
            gpu.Version = _mainThreadData.GraphicsDeviceVersion;
            gpu.ApiType = _mainThreadData.GraphicsDeviceType;
            gpu.MaxTextureSize = _mainThreadData.MaxTextureSize;
            gpu.SupportsDrawCallInstancing = _mainThreadData.SupportsDrawCallInstancing;
            gpu.SupportsRayTracing = _mainThreadData.SupportsRayTracing;
            gpu.SupportsComputeShaders = _mainThreadData.SupportsComputeShaders;
            gpu.SupportsGeometryShaders = _mainThreadData.SupportsGeometryShaders;

            gpu.VendorId = SafeLazyUnwrap(_mainThreadData.GraphicsDeviceVendorId, nameof(gpu.VendorId));
            gpu.MultiThreadedRendering = SafeLazyUnwrap(_mainThreadData.GraphicsMultiThreaded, nameof(gpu.MultiThreadedRendering));

            if (_mainThreadData.GraphicsShaderLevel.HasValue && _mainThreadData.GraphicsShaderLevel != -1)
            {
                gpu.GraphicsShaderLevel = ToGraphicShaderLevelDescription(_mainThreadData.GraphicsShaderLevel.Value);
            }

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

        private void PopulateUnity(Protocol.Unity unity)
        {
            unity.InstallMode = _mainThreadData.InstallMode;
        }

        private void PopulateTags(SentryEvent @event)
        {
            if (_mainThreadData.InstallMode is { } installMode)
            {
                @event.SetTag("unity.install_mode", installMode);
            }

            if (_mainThreadData.SupportsDrawCallInstancing.HasValue)
            {
                @event.SetTag("unity.gpu.supports_instancing", _mainThreadData.SupportsDrawCallInstancing.Value ? "true" : "false");
            }

            if (_mainThreadData.DeviceType is not null && _mainThreadData.DeviceType.IsValueCreated)
            {
                @event.SetTag("unity.device.device_type", _mainThreadData.DeviceType.Value);
            }

            if (_sentryOptions.SendDefaultPii && _mainThreadData.DeviceUniqueIdentifier is not null && _mainThreadData.DeviceUniqueIdentifier.IsValueCreated)
            {
                @event.SetTag("unity.device.unique_identifier", _mainThreadData.DeviceUniqueIdentifier.Value);
            }
        }

        /// <summary>
        /// - If UI thread, extract the value (can be null)
        /// - If non-UI thread, check if value is created, then extract
        /// - 'null' otherwise
        /// </summary>
        private string? SafeLazyUnwrap(Lazy<string>? lazyValue, string? propertyName = null)
        {
            if (lazyValue == null)
            {
                return null;
            }

            if (_mainThreadData.IsMainThread())
            {
                return lazyValue.Value;
            }

            if (lazyValue.IsValueCreated)
            {
                return lazyValue.Value;
            }

            if (propertyName is not null)
            {
                _sentryOptions.DiagnosticLogger?.LogDebug("Not UI thread. Value hasn't been unwrapped yet, returning 'null' for property: {0}", propertyName);
            }

            return null;
        }

        /*
         * Can't be made generic. At the time of writing, you can't specify if 'T' is nullable for 'struct' and 'class' at the same time.
         * Check https://github.com/dotnet/csharplang/discussions/3060 and https://github.com/dotnet/csharplang/blob/main/meetings/2019/LDM-2019-11-25.md
         */
        private bool? SafeLazyUnwrap(Lazy<bool>? lazyValue, string? propertyName = null)
        {
            if (lazyValue == null)
            {
                return null;
            }

            if (_mainThreadData.IsMainThread())
            {
                return lazyValue.Value;
            }

            if (lazyValue.IsValueCreated)
            {
                return lazyValue.Value;
            }

            if (propertyName is not null)
            {
                _sentryOptions.DiagnosticLogger?.LogDebug("Not UI thread. Value hasn't been unwrapped yet, returning 'null' for property: {0}", propertyName);
            }

            return null;
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
