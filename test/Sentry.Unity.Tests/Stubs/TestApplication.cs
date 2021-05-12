using System;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Tests.Stubs
{
    internal sealed class TestApplication : IApplication
    {
        public event Application.LogCallback? LogMessageReceived;
        public event Action? Quitting;
        public string ActiveSceneName => "TestSceneName";
    }
}
