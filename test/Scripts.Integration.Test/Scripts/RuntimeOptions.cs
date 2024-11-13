using Sentry;
using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/RuntimeOptions.cs", menuName = "Sentry/RuntimeOptions", order = 999)]
public class RuntimeOptions : SentryRuntimeOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: RuntimeOptions::Configure() called");

        options.Dsn = "Old configure got called";

        Debug.Log("Sentry: RuntimeOptions::Configure() finished");
    }
}
