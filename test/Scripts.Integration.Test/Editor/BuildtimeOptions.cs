using Sentry;
using Sentry.Unity;
using Sentry.Unity.Editor;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/BuildTimeOptions.cs", menuName = "Sentry/BuildTimeOptions", order = 999)]
public class BuildTimeOptions : SentryBuildTimeOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: BuildTimeOptions::Configure() called");

        switch (EditorUserBuildSettings.selectedBuildTargetGroup)
        {
            case BuildTargetGroup.Android:
                options.Dsn = "http://publickey@10.0.2.2:8000/12345";
                break;
            case BuildTargetGroup.WebGL:
                options.Dsn = "http://publickey@127.0.0.1:8000/12345";
                break;
            default:
                options.Dsn = "http://publickey@localhost:8000/12345";
                break;
        }

        Debug.LogFormat("Sentry: Setting options.Dsn = {0}", options.Dsn);

        options.Il2CppLineNumberSupportEnabled = true;
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;
        options.PerformanceAutoInstrumentationEnabled = true;

        cliOptions.UploadSymbols = !string.IsNullOrEmpty(cliOptions.UrlOverride);
        cliOptions.UploadSources = cliOptions.UploadSymbols;
        cliOptions.Organization = "sentry-sdks";
        cliOptions.Project = "sentry-unity";
        cliOptions.Auth = "dummy-token";

        Debug.Log("Sentry: BuildTimeOptions::Configure() finished");
    }
}
