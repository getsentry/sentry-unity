using Sentry;
using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SmokeTestOptions.cs", menuName = "Sentry/SmokeTestOptions", order = 999)]
public class SmokeTestOptions : ScriptableOptionsConfiguration
{
    public override void ConfigureAtBuild(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: SmokeTestOptions::ConfigureAtBuild() called");

        cliOptions.UploadSymbols = !string.IsNullOrEmpty(cliOptions.UrlOverride);
        cliOptions.UploadSources = cliOptions.UploadSymbols;
        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity";
        cliOptions.Auth = "dummy-token";

        Debug.Log("Sentry: SmokeTestOptions::ConfigureAtBuild() finished");
    }

    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {
        Debug.Log("Sentry: SmokeTestOptions::ConfigureAtRuntime() called");
        Configure(options);

        options.CreateHttpClientHandler = () => SmokeTester.t;
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

        Debug.Log("Sentry: SmokeTestOptions::ConfigureAtRuntime() finished");
    }

    /// Usually, it's a good idea to have common configuration and only call really specific setup in the above methods.
    private void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: SmokeTestOptions::Configure() called");

        options.Dsn = string.Format("http://publickey@{0}:8000/12345",
#if UNITY_ANDROID
            "10.0.2.2");
#elif UNITY_WEBGL
            "127.0.0.1");
#else
            "localhost");
#endif

        options.AttachScreenshot = true;
        options.Il2CppLineNumberSupportEnabled = true;
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;
        options.PerformanceAutoInstrumentationEnabled = true;
        Debug.Log("Sentry: SmokeTestOptions::Configure() finished");
    }
}
