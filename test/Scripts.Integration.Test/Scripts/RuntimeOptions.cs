using Sentry;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.Assertions;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/RuntimeOptions.cs", menuName = "Sentry/RuntimeOptions", order = 999)]
public class RuntimeOptions : SentryRuntimeOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: RuntimeOptions::Configure() called");

        // Make sure the config is still called and the DSN has been set
        Assert.IsFalse(string.IsNullOrWhiteSpace(options.Dsn));

        Debug.Log("Sentry: RuntimeOptions::Configure() finished");
    }
}
