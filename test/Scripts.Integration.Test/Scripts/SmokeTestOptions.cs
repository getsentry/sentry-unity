using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SmokeTestOptions.cs", menuName = "Sentry/SmokeTestOptions", order = 999)]
public class SmokeTestOptions : ScriptableOptionsConfiguration
{
    public override void ConfigureAtBuild(SentryUnityOptions options)
    {
        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtBuild called");
    }
    public override void ConfigureAtRuntime(SentryUnityOptions options)
    {
        Debug.Log("Sentry: SmokeTestOptions - ConfigureAtRuntime called");
        SmokeTester.Configure(options);
    }
}