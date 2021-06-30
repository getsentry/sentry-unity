using System;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Integrations
{
    internal interface IApplication
    {
        event Application.LogCallback LogMessageReceived;
        event Action Quitting;
        string ActiveSceneName { get; }
        bool IsEditor { get; }
        string ProductName { get; }
        string Version { get; }
        string PersistentDataPath { get; }
    }

    internal sealed class ApplicationAdapter : IApplication
    {
        public static readonly ApplicationAdapter Instance = new();

        private ApplicationAdapter()
        {
            Application.logMessageReceived += OnLogMessageReceived;
            Application.quitting += OnQuitting;
        }

        public event Application.LogCallback? LogMessageReceived;

        public event Action? Quitting;

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public bool IsEditor => Application.isEditor;

        public string ProductName => Application.productName;

        public string Version => Application.version;

        public string PersistentDataPath => Application.persistentDataPath;

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
            => LogMessageReceived?.Invoke(condition, stackTrace, type);

        private void OnQuitting()
            => Quitting?.Invoke();
    }
}
