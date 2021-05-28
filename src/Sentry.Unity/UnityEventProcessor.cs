using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Reflection;
using Sentry.Unity.Integrations;
using UnityEngine;
using DeviceOrientation = Sentry.Protocol.DeviceOrientation;

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
        private readonly IApplication _application;

        public UnityEventProcessor(IApplication? application = null)
        {
            _application = application ?? ApplicationAdapter.Instance;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            @event.Sdk.AddPackage(UnitySdkInfo.PackageName, UnitySdkInfo.Version);
            @event.Sdk.Name = UnitySdkInfo.Name;
            @event.Sdk.Version = UnitySdkInfo.Version;

            @event.Contexts.OperatingSystem.Name = SystemInfo.operatingSystem;

            @event.Contexts.Device.Name = SystemInfo.deviceName;
#pragma warning disable RECS0018 // Value is exact when expressing no battery level
            if (SystemInfo.batteryLevel != -1.0)
#pragma warning restore RECS0018
            {
                @event.Contexts.Device.BatteryLevel = (short?)(SystemInfo.batteryLevel * 100);
            }

            @event.ServerName = null;

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            @event.Contexts.Device.MemorySize = SystemInfo.systemMemorySize;

            @event.Contexts.Device.Timezone = TimeZoneInfo.Local;

            @event.Contexts.App.StartTime = DateTimeOffset.UtcNow.AddSeconds(-Time.realtimeSinceStartup);

            // TODO:  @event.Contexts["Unity"] = new UnityContext (values to be read on the main thread)
            @event.SetExtra("unity:processorCount", SystemInfo.processorCount.ToString());
            @event.SetExtra("unity:supportsVibration", SystemInfo.supportsVibration.ToString());
            @event.SetExtra("unity:installMode", Application.installMode.ToString());

            // TODO: Will move to raw_description once parsing is done in Sentry
            @event.Contexts.OperatingSystem.Name = SystemInfo.operatingSystem;

            switch (Input.deviceOrientation)
            {
                case UnityEngine.DeviceOrientation.Portrait:
                case UnityEngine.DeviceOrientation.PortraitUpsideDown:
                    @event.Contexts.Device.Orientation = DeviceOrientation.Portrait;
                    break;
                case UnityEngine.DeviceOrientation.LandscapeLeft:
                case UnityEngine.DeviceOrientation.LandscapeRight:
                    @event.Contexts.Device.Orientation = DeviceOrientation.Landscape;
                    break;
                case UnityEngine.DeviceOrientation.FaceUp:
                case UnityEngine.DeviceOrientation.FaceDown:
                    // TODO: Add to protocol?
                    break;
            }

            var model = SystemInfo.deviceModel;
            if (model != SystemInfo.unsupportedIdentifier
                // Returned by the editor
                && model != "System Product Name (System manufacturer)")
            {
                @event.Contexts.Device.Model = model;
            }

            //device.DeviceType = SystemInfo.deviceType.ToString();
            //device.CpuDescription = SystemInfo.processorType;
            //device.BatteryStatus = SystemInfo.batteryStatus.ToString();

            @event.SetExtra("unity:batteryStatus", SystemInfo.batteryStatus.ToString());
            @event.SetExtra("unity:deviceType", SystemInfo.deviceType.ToString());
            @event.SetExtra("unity:processorType", SystemInfo.processorType);

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            if (SystemInfo.systemMemorySize != 0)
            {
                @event.Contexts.Device.MemorySize = SystemInfo.systemMemorySize * 1048576L; // Sentry device mem is in Bytes
            }

            @event.Contexts.Gpu.Id = SystemInfo.graphicsDeviceID;
            @event.Contexts.Gpu.Name = SystemInfo.graphicsDeviceName;
            @event.Contexts.Gpu.VendorId = SystemInfo.graphicsDeviceVendorID.ToString();
            @event.Contexts.Gpu.VendorName = SystemInfo.graphicsDeviceVendor;
            @event.Contexts.Gpu.MemorySize = SystemInfo.graphicsMemorySize;
            @event.Contexts.Gpu.MultiThreadedRendering = SystemInfo.graphicsMultiThreaded;
            @event.Contexts.Gpu.NpotSupport = SystemInfo.npotSupport.ToString();
            @event.Contexts.Gpu.Version = SystemInfo.graphicsDeviceVersion;
            @event.Contexts.Gpu.ApiType = SystemInfo.graphicsDeviceType.ToString();

            @event.Contexts.App.StartTime = DateTimeOffset.UtcNow
                // NOTE: Time API requires main thread
                .AddSeconds(-Time.realtimeSinceStartup);

            if (Debug.isDebugBuild)
            {
                @event.Contexts.App.BuildType = "debug";
            }
            else
            {
                @event.Contexts.App.BuildType = "release";
            }

            // The app can be run in an iOS or Android emulator. We can't safely set a value for simulator.
            @event.Contexts.Device.Simulator = _application.IsEditor ? true : null;

            return @event;
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
