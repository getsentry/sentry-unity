using System.Collections.Generic;
using Sentry;
using Sentry.Unity;
using UnityEngine;

public class OptionsConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: OptionsConfig::Configure() called");

        string host;
        
#if UNITY_EDITOR        
        // Workaround for an issue specific to Unity 6.0 where in CI, `UNITY_ANDROID` would resolve to `false` during the build
        if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.Android)
        {
            host = "10.0.2.2";
        }
        else if (UnityEditor.EditorUserBuildSettings.activeBuildTarget == UnityEditor.BuildTarget.WebGL)
        {
            host = "127.0.0.1";
        }
        else
        {
            host = "localhost";
        }
#else

#if UNITY_ANDROID
        host = "10.0.2.2";
#elif UNITY_WEBGL
        host = "127.0.0.1";
#else
        host = "localhost";
#endif
#endif

        options.Dsn = $"http://publickey@{host}:8000/12345";

        Debug.LogFormat("Sentry: Setting options.Dsn = {0}", options.Dsn);

        options.AttachScreenshot = true;
        options.Il2CppLineNumberSupportEnabled = true;
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;
        options.PerformanceAutoInstrumentationEnabled = true;

        options.CreateHttpMessageHandler = () => SmokeTester.t;
        SmokeTester.CrashedLastRun = () =>
        {
            if (options.CrashedLastRun != null)
            {
                return options.CrashedLastRun() ? 1 : 0;
            }
            return -2;
        };

        // Filtering the SmokeTester logs from the breadcrumbs here
        options.AddBreadcrumbsForLogType = new Dictionary<LogType, bool>
        {
            { LogType.Error, true},
            { LogType.Assert, true},
            { LogType.Warning, true},
            { LogType.Log, false}, // No breadcrumbs for Debug.Log
            { LogType.Exception, true},
        };

        // If an ANR triggers while the smoke test runs, the test would fail because we expect exact order of events.
        options.DisableAnrIntegration();

        // These options will get overwritten by CI. This allows us to create artifacts for both initialization types.
        options.AndroidNativeInitializationType = NativeInitializationType.Runtime;
        options.IosNativeInitializationType = NativeInitializationType.Runtime;

        Debug.Log("Sentry: OptionsConfig::Configure() finished");
    }
}
