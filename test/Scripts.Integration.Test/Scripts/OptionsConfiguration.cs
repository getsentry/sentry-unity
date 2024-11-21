using Sentry;
using Sentry.Unity;
using UnityEngine;

public class OptionsConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: OptionsConfig::Configure() called");

        options.Dsn = "http://publickey@"
#if UNITY_ANDROID
            + "10.0.2.2"
#elif UNITY_WEBGL
            + "127.0.0.1"
#else
            + "localhost"
#endif
            + ":8000/12345";

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

        // If an ANR triggers while the smoke test runs, the test would fail because we expect exact order of events.
        options.DisableAnrIntegration();

        Debug.Log("Sentry: BuildTimeOptions::Configure() finished");
    }
}
