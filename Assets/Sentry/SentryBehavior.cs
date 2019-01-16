using System;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity
{
    public class SentryBehavior : MonoBehaviour
    {
        private static IDisposable _sdk;
        private static long _timeLastError;
        public static long MinTime { get; } = TimeSpan.FromMilliseconds(500).Ticks;
        private static SentryBehavior _sentryBehavior;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnRuntimeMethodLoad()
        {
            var dsn = // null; // **SET YOUR DSN HERE**
                "https://5fd7a6cda8444965bade9ccfd3df9882@sentry.io/1188141";

            if (dsn == null)
            {
                Debug.LogError("No DSN set!");
                return;
            }
          
            _sdk = SentrySdk.Init(o =>
            {
                o.Dsn = new Dsn(dsn);
                o.Debug = true;

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
        }

        public void Disable()
        {
           Application.logMessageReceived -= OnLogMessageReceived;
           SentrySdk.Close();
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
        private void OnApplicationQuit() => _sdk?.Dispose();

        // TODO: Flush events. See note on OnApplicationQuit
        //private void OnApplicationPause() => 
        // TODO: Flush events, see note on OnApplicationQuit
        // private void OnApplicationFocus { if (!focusStatus) Flush events! }
    }
}