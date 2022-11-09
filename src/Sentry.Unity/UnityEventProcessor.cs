using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using UnityEngine;
using DeviceOrientation = Sentry.Protocol.DeviceOrientation;

namespace Sentry.Unity
{
    internal class UnityEventProcessor :
        ISentryEventProcessor,
        ISentryTransactionProcessor
    {
        private readonly SentryUnityOptions _sentryOptions;
        private readonly MainThreadData _mainThreadData;

        public UnityEventProcessor(SentryUnityOptions sentryOptions, SentryMonoBehaviour sentryMonoBehaviour)
        {
            _sentryOptions = sentryOptions;
            _mainThreadData = sentryMonoBehaviour.MainThreadData;
        }

        public Transaction Process(Transaction transaction)
        {
            SetEventContext(transaction);
            return transaction;
        }

        public SentryEvent Process(SentryEvent @event)
        {
            SetEventContext(@event);

            @event.ServerName = null;

            return @event;
        }

        private void SetEventContext(IEventLike sentryEvent)
        {
            try
            {
                PopulateDevice(sentryEvent.Contexts.Device);
                // Populating the SDK Integrations here (for now) instead of UnityScopeIntegration because it cannot be guaranteed
                // that it got added last or that there was not an integration added at a later point
                PopulateSdkIntegrations(sentryEvent.Sdk);
                // TODO revisit which tags we should be adding by default
                sentryEvent.SetTag("unity.is_main_thread", _mainThreadData.IsMainThread().ToTagValue());
            }
            catch (Exception exception)
            {
                _sentryOptions.DiagnosticLogger?.LogError("{0} processing failed.", exception, nameof(SentryEvent));
            }
        }

        private void PopulateDevice(Device device)
        {
            if (!_mainThreadData.IsMainThread())
            {
                return;
            }

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

        private void PopulateSdkIntegrations(SdkVersion sdkVersion)
        {
            foreach (var integrationName in _sentryOptions.SdkIntegrationNames)
            {
                sdkVersion.AddIntegration(integrationName);
            }
        }
    }

    internal static class TagValueNormalizer
    {
        internal static string ToTagValue(this bool value) => value ? "true" : "false";
    }
}
