using Sentry;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using Sentry.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    // https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417
    [Serializable]
    public sealed class UnitySentryOptions : ScriptableObject
    {
        public void OnEnable() => hideFlags = HideFlags.DontUnloadUnusedAsset;
        [field: SerializeField] public bool Enabled { get; set; } = true;
        [field: SerializeField] public string Dsn { get; set; }
        [field: SerializeField] public bool Debug { get; set; } = true; // By default on only
        [field: SerializeField] public bool DebugOnlyInEditor { get; set; } = true;
        [field: SerializeField] public SentryLevel DiagnosticsLevel { get; set; } = SentryLevel.Error; // By default logs out Error or higher
        [field: SerializeField] public float SampleRate { get; set; } = 1.0f;
    }

    public static class SentryInitialization
    {
        public static SentryOptions Options;

        private static long _timeLastError;
        public static long MinTime { get; } = TimeSpan.FromMilliseconds(500).Ticks;

        // TODO: Take SentryOptions from UnitySettings

        // Needs to be on if now platform specific init code is required
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = Resources.Load("Sentry/SentryOptions") as UnitySentryOptions;
            var sentryInEditor = true; // Make this configurable

            if (Application.isEditor && !sentryInEditor)
            {
                Debug.Log("Sentry SDK disabled.");
                return;
            }

            var dsn = options?.Dsn;

            _ = SentrySdk.Init(o =>
            {
                o.Dsn = dsn;

                if (options.Debug
                    && (!options.DebugOnlyInEditor || Application.isEditor))
                {
                    o.Debug = true;
                    o.DiagnosticLogger = new UnityLogger(options.DiagnosticsLevel);
                    o.DiagnosticsLevel = options.DiagnosticsLevel;
                }

                // Uses the game `version` as Release
                o.Release = Application.version;

                // TODO: take Environment from configuration. Default is `production` from within the SDK.
                if (Application.isEditor)
                {
                    o.Environment = "editor";
                }

                // If PDBs are available, CaptureMessage also includes a stack trace
                o.AttachStacktrace = true;

                // Required configurations to integrate with Unity
                o.AddInAppExclude("UnityEngine");
                o.AddInAppExclude("UnityEditor");
                // Some targets doesn't support GZipping the events sent out
                // TODO: Disable it selectively
                o.RequestBodyCompressionLevel = System.IO.Compression.CompressionLevel.NoCompression;
                o.AddEventProcessor(new UnityEventProcessor());
                o.AddExceptionProcessor(new UnityEventExceptionProcessor());
            });

            // TODO: Consider ensuring this code path doesn't require UI thread
            // Then use logMessageReceivedThreaded instead
            Application.logMessageReceived += OnLogMessageReceived;

            Application.quitting += OnApplicationQuit;

            SentrySdk.AddBreadcrumb("BeforeSceneLoad",
                data: new Dictionary<string, string> { { "scene", SceneManager.GetActiveScene().name } });

            // We don't need it anymore and it won't be GC'ed because of its HideFlags
            // ScriptableObject.DestroyImmediate(options, true);
            Debug.Log("Sentry initialized");
        }

        // Happens with Domain Reloading

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration()
        {
            SentrySdk.AddBreadcrumb("SubsystemRegistration");
        }

        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!SentrySdk.IsEnabled)
            {
                Debug.LogError("Cannot handle log message if we are not initialized");
                return;
            }

            var time = DateTime.UtcNow.Ticks;

            if (time - _timeLastError <= MinTime)
            {
                return;
            }

            _timeLastError = time;

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // TODO: MinBreadcrumbLevel
                SentrySdk.AddBreadcrumb(logString, level: ToBreadcrumbLevel(type));
                return;
            }

            var evt = new SentryEvent(new UnityLogException(logString, stackTrace));
            evt.SetTag("log.type", ToEventTagType(type));
            _ = SentrySdk.CaptureEvent(evt);
            SentrySdk.AddBreadcrumb(logString, level: ToBreadcrumbLevel(type));
        }

        // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
        //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
        // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
        // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.

        private static void OnApplicationQuit()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            SentrySdk.Close();
            Debug.Log("Sentry SDK disposed.");
        }

        private static string ToEventTagType(LogType type) =>
            type switch
            {
                LogType.Assert => "assert",
                LogType.Error => "error",
                LogType.Exception => "exception",
                LogType.Log => "log",
                LogType.Warning => "warning",
                _ => "unknown"
            };

        private static BreadcrumbLevel ToBreadcrumbLevel(LogType type) =>
            type switch
            {
                LogType.Assert => BreadcrumbLevel.Error,
                LogType.Error => BreadcrumbLevel.Error,
                LogType.Exception => BreadcrumbLevel.Error,
                LogType.Log => BreadcrumbLevel.Info,
                LogType.Warning => BreadcrumbLevel.Warning,
                _ => BreadcrumbLevel.Info
            };
    }
}

