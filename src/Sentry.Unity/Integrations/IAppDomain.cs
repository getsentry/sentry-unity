using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Integrations
{
    internal interface IAppDomain
    {
        event Application.LogCallback LogMessageReceived;
        event Action Quitting;
        string ActiveSceneName { get; }
    }

    internal sealed class UnityAppDomain : IAppDomain
    {
        public static readonly UnityAppDomain Instance = new();

        private UnityAppDomain()
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
