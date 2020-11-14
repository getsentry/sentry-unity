using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity
{
    [Serializable]
    public sealed class UnitySentryOptions : ScriptableObject
    {
        [field: SerializeField] public bool Enabled { get; set; } = true;
        [field: SerializeField] public bool CaptureInEditor { get; set; } = true; // Lower entry barrier, likely set to false after initial setup.
        [field: SerializeField] public string? Dsn { get; set; }
        [field: SerializeField] public bool Debug { get; set; } = true; // By default on only
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; } = true;
        [field: SerializeField] public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher.
        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;

        [field: NonSerialized] public IDiagnosticLogger? Logger { get; private set; }

        public void OnEnable()
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset;
            Logger = Debug
                     && (!DebugOnlyInEditor || Application.isEditor)
                ? new UnityLogger(DiagnosticsLevel)
                : null;
        }

        private class GuardedLogger : IDiagnosticLogger
        {
            private readonly IDiagnosticLogger _logger;

            public GuardedLogger(IDiagnosticLogger logger) => _logger = logger;

            public bool IsEnabled(SentryLevel level) => _logger?.IsEnabled(level) == true;

            public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
            {
                if (IsEnabled(logLevel))
                {
                    _logger.Log(logLevel, message, exception, args);
                }
            }
        }
    }
}
