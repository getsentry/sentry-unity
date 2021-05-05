using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Integrations
{
    internal interface IApplication
    {
        event Application.LogCallback LogMessageReceived;
        event Action Quitting;
        string ActiveSceneName { get; }
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

        private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
            => LogMessageReceived?.Invoke(condition, stackTrace, type);

        private void OnQuitting()
            => Quitting?.Invoke();
    }
}
