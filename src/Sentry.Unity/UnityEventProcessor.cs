using System;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Unity.Integrations;
using UnityEngine;
using DeviceOrientation = Sentry.Protocol.DeviceOrientation;

namespace Sentry.Unity;

internal class UnityEventProcessor :
    ISentryEventProcessor,
    ISentryTransactionProcessor
{
    private readonly SentryUnityOptions _sentryOptions;
    private readonly ISentryUnityInfo _unityInfo;
    private readonly ISceneManager _sceneManager;
    private readonly IApplication _application;

    public UnityEventProcessor(SentryUnityOptions sentryOptions, ISentryUnityInfo unityInfo, IApplication? application = null, ISceneManager? sceneManager = null)
    {
        _sentryOptions = sentryOptions;
        _unityInfo = unityInfo;
        _application = application ?? ApplicationAdapter.Instance;
        _sceneManager = sceneManager ?? SceneManagerAdapter.Instance;
    }

    public SentryTransaction Process(SentryTransaction transaction)
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
            PopulateApp(sentryEvent.Contexts.App);
            PopulateDevice(sentryEvent.Contexts.Device);

            // The Unity context should get set in the UnityScopeIntegration automatically sets it when it gets registered
            sentryEvent.Contexts.TryGetValue(Protocol.Unity.Type, out var contextObject);
            if (contextObject is not Protocol.Unity unityContext)
            {
                unityContext = new Protocol.Unity();
                sentryEvent.Contexts.Add(Protocol.Unity.Type, unityContext);
            }

            PopulateUnity(unityContext);

            // Populating the SDK Integrations here (for now) instead of UnityScopeIntegration because it cannot be guaranteed
            // that it got added last or that there was not an integration added at a later point
            PopulateSdkIntegrations(sentryEvent.Sdk);
        }
        catch (Exception exception)
        {
            _sentryOptions.DiagnosticLogger?.LogError(exception: exception, "{0} processing failed.", nameof(SentryEvent));
        }
    }

    private void PopulateApp(App app)
    {
        if (!MainThreadData.IsMainThread())
        {
            return;
        }

        // The Profiler returns '0' if it is not available
        var totalAllocatedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        if (totalAllocatedMemory > 0)
        {
            app.Memory = totalAllocatedMemory;
        }
    }

    private void PopulateDevice(Device device)
    {
        if (!MainThreadData.IsMainThread())
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

    private void PopulateUnity(Protocol.Unity unity)
    {
        unity.IsMainThread = MainThreadData.IsMainThread();

        if (!MainThreadData.IsMainThread())
        {
            return;
        }

        if (_application.IsEditor || _unityInfo.IL2CPP)
        {
            // Currently an IL2CPP only feature: see https://github.com/getsentry/sentry-unity/issues/2181
            unity.ActiveSceneName = _sceneManager.GetActiveScene().Name;
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
