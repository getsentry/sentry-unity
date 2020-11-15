using System;
using Sentry.Extensibility;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    public enum SentryUnityCompression
    {
        Auto = 0,
        Optimal = 1,
        Fastest = 2,
        NoCompression = 3
    }
    [Serializable]
    public sealed class UnitySentryOptions : ScriptableObject
    {
        [field: SerializeField] public bool Enabled { get; set; } = true;
        [field: SerializeField] public bool CaptureInEditor { get; set; } = true; // Lower entry barrier, likely set to false after initial setup.
        [field: SerializeField] public string? Dsn { get; set; }
        [field: SerializeField] public bool Debug { get; set; } = true; // By default on only
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; } = true;
        [field: SerializeField] public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher.
        // Ideally this would be per platform
        // Auto allows us to try figure out things in the SDK depending on the platform. Any other value means an explicit user choice.
        [field: SerializeField] public SentryUnityCompression RequestBodyCompressionLevel { get; set; } = SentryUnityCompression.Auto;
        [field: SerializeField] public bool AttachStacktrace { get; set; }
        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;

        [field: NonSerialized] public IDiagnosticLogger? Logger { get; private set; }
        [field: NonSerialized] public string? Release { get; set; }
        [field: NonSerialized] public string? Environment { get; set; }

        public void OnEnable()
        {
            hideFlags = HideFlags.DontUnloadUnusedAsset;
            Logger = Debug
                     && (!DebugOnlyInEditor || Application.isEditor)
                ? new UnityLogger(DiagnosticsLevel)
                : null;
        }
    }
}
