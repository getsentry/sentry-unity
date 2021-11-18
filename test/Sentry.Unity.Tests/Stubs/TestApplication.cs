using System;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Tests.Stubs
{
    public sealed class TestApplication : IApplication
    {
        public TestApplication(
            bool isEditor = true,
            string productName = "",
            string version = "",
            string persistentDataPath = "",
            RuntimePlatform platform = RuntimePlatform.WindowsEditor)
        {
            IsEditor = isEditor;
            ProductName = productName;
            Version = version;
            PersistentDataPath = persistentDataPath;
            Platform = platform;
        }

        public event Application.LogCallback? LogMessageReceived;
        public event Action? Quitting;
        public string ActiveSceneName => "TestSceneName";
        public bool IsEditor { get; }
        public string ProductName { get; }
        public string Version { get; }
        public string PersistentDataPath { get; }
        public RuntimePlatform Platform { get; }
        private void OnQuitting() => Quitting?.Invoke();

        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
            => LogMessageReceived?.Invoke(condition, stacktrace, type);
    }
}
