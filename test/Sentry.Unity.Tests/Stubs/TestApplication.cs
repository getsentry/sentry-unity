using System;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.TestTools.Constraints;

namespace Sentry.Unity.Tests.Stubs
{
    internal sealed class TestApplication : IApplication
    {
        public TestApplication(bool isEditor = true)
        {
            IsEditor = isEditor;
        }

        public event Application.LogCallback? LogMessageReceived;
        public event Action? Quitting;
        public string ActiveSceneName => "TestSceneName";
        public bool IsEditor { get; }

        private void OnQuitting() => Quitting?.Invoke();

        private void OnLogMessageReceived(string condition, string stacktrace, LogType type)
            => LogMessageReceived?.Invoke(condition, stacktrace, type);
    }
}
