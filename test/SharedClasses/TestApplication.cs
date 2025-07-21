using System;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Tests.Stubs;

public sealed class TestApplication : IApplication
{
    public TestApplication(
        bool isEditor = true,
        string productName = "",
        string version = "",
        string buildGUID = "",
        string unityVersion = "",
        string persistentDataPath = "",
        RuntimePlatform platform = RuntimePlatform.WindowsEditor)
    {
        IsEditor = isEditor;
        ProductName = productName;
        Version = version;
        BuildGUID = buildGUID;
        UnityVersion = unityVersion;
        PersistentDataPath = persistentDataPath;
        Platform = platform;
    }

    public event Application.LogCallback? LogMessageReceived;
    public event Action? Quitting;
    public string ActiveSceneName => "TestSceneName";
    public bool IsEditor { get; set; }
    public string ProductName { get; }
    public string Version { get; }
    public string BuildGUID { get; }
    public string UnityVersion { get; set; }
    public string PersistentDataPath { get; set; }
    public RuntimePlatform Platform { get; set; }
    private void OnQuitting() => Quitting?.Invoke();

    private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
        => LogMessageReceived?.Invoke(condition, stacktrace, type);
}
