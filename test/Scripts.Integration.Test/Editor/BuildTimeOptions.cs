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

        // Assert the deprecated options have been set and are getting overwritten
        options.Dsn = "Old configure got called";
        cliOptions.Auth = "Old configure got called";

        Debug.Log("Sentry: BuildTimeOptions::Configure() finished");
    }
}
