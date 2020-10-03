using Sentry;
using Sentry.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    internal static class SentryInitialization
    {
        private static long _timeLastError;
        public static long MinTime { get; } = TimeSpan.FromMilliseconds(500).Ticks;


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BeforeSceneLoad()
        {
            bool sentryInEditor = true; // Make this configurable
            if (Application.isEditor && !sentryInEditor)
            {
                Debug.Log("Sentry SDK disabled.");
                return;
            }
            Debug.Log("Initializing Sentry.");

            var dsn = // null; // **SET YOUR DSN HERE**
                "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";

            if (dsn == null)
            {
                Debug.LogWarning("No Sentry DSN set!");
                return;
            }

            SentrySdk.Init(o =>
            {
                o.Dsn = new Dsn(dsn);

                // read from config
                if (Application.isEditor)
                {
                    // NOTE: This is simply to see the internal logging of the SDK
                    // A production situation would NOT have this enabled.
                    o.Debug = true;
                }

                // Uses the game `version` as Release
                o.Release = Application.version;
                // If PDBs are available, CaptureMessage also includes a stack trace
                o.AttachStacktrace = true;

                // Required configurations to integrate with Unity
                o.AddInAppExclude("UnityEngine");
                o.DiagnosticLogger = new UnityLogger();
                // Some targets doesn't support GZipping the events sent out
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

            Debug.Log("Sentry initialized");
        }


        private static void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!SentrySdk.IsEnabled)
            {
                Debug.LogError("Cannot handle log message if we are not initialized");
                return;
            }

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // only send errors, can be set somewhere what we send and what we don't
                if (type == LogType.Warning)
                {
                    SentrySdk.AddBreadcrumb(logString, level: BreadcrumbLevel.Warning);
                }
                return;
            }

            var time = DateTime.UtcNow.Ticks;

            if (time - _timeLastError <= MinTime)
            {
                return;
            }

            _timeLastError = time;

            SentrySdk.CaptureEvent(new SentryEvent(new UnityLogException(logString, stackTrace)));
        }


        // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call. 
        //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
        // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
        // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.

        private static void OnApplicationQuit()
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            SentrySdk.Close();
            Debug.Log("Sentry sdk disposed.");
        }
    }
}
