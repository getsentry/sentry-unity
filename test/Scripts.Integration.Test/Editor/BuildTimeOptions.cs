using Sentry;
using Sentry.Unity;
using Sentry.Unity.Editor;
using UnityEngine;
using UnityEditor;
using UnityEngine.Assertions;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/BuildTimeOptions.cs", menuName = "Sentry/BuildTimeOptions", order = 999)]
public class BuildTimeOptions : SentryBuildTimeOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options, SentryCliOptions cliOptions)
    {
        Debug.Log("Sentry: BuildTimeOptions::Configure() called");

        // Make sure the config is still called and the DSN has been set
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Dsn));
        Assert.IsFalse(string.IsNullOrWhiteSpace(cliOptions.Auth));
        
        Debug.Log("Sentry: BuildTimeOptions::Configure() finished");
    }
}
