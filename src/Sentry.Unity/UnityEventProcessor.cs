using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using UnityEngine;
using DeviceOrientation = Sentry.Protocol.DeviceOrientation;

namespace Sentry.Unity
{
    internal class UnityEventProcessor : ISentryEventProcessor
    {
        private readonly SentryUnityOptions _sentryOptions;
        private readonly MainThreadData _mainThreadData;


        public UnityEventProcessor(SentryUnityOptions sentryOptions, SentryMonoBehaviour sentryMonoBehaviour)
        {
            _sentryOptions = sentryOptions;
            _mainThreadData = sentryMonoBehaviour.MainThreadData;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            try
            {
                PopulateDevice(@event.Contexts.Device);
                // TODO revisit which tags we should be adding by default
                @event.SetTag("unity.is_main_thread", _mainThreadData.IsMainThread().ToTagValue());
            }
            catch (Exception ex)
            {
                _sentryOptions.DiagnosticLogger?.LogError("{0} processing failed.", ex, nameof(SentryEvent));
            }

            @event.ServerName = null;

            return @event;
        }

        private void PopulateDevice(Device device)
        {
            if (_mainThreadData.IsMainThread())
            {
                device.BatteryStatus = SystemInfo.batteryStatus.ToString();

                var batteryLevel = SystemInfo.batteryLevel;
                if (batteryLevel > 0.0)
                {
                    device.BatteryLevel = (short?)(batteryLevel * 100);
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

    }

    internal class UnityEventExceptionProcessor : ISentryEventExceptionProcessor
    {
        public void Process(Exception exception, SentryEvent sentryEvent)
        {
        }
    }

    internal static class TagValueNormalizer
    {
        internal static string ToTagValue(this Boolean value) => value ? "true" : "false";
    }
}
