using Sentry;
using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SmokeTestOptions.cs", menuName = "Sentry/SmokeTestOptions", order = 999)]
public class SmokeTestOptions : ScriptableOptionsConfiguration
{
    public override void ConfigureAtBuild(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtBuild called");

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
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;
        options.PerformanceAutoInstrumentationEnabled = true;

        cliOptions.UploadSymbols = !string.IsNullOrEmpty(cliOptions.UrlOverride);
        cliOptions.UploadSources = cliOptions.UploadSymbols;
        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity";
        cliOptions.Auth = "dummy-token";

        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtBuild finished");
    }

    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {
        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtRuntime called");
        SmokeTester.Configure(options);
        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtRuntime finished");
    }
}
