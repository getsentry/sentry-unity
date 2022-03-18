using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/SmokeTestOptions.cs", menuName = "Sentry/SmokeTestOptions", order = 999)]
public class SmokeTestOptions : ScriptableOptionsConfiguration
{
    // This method gets called when you instantiated the scriptable object and added it to the configuration window
    public override void Configure(SentryUnityOptions options)
    {
        // NOTE: Native support is already initialized by the time this method runs, so Unity bugs are captured.
        // That means changes done to the 'options' here will only affect events from C# scripts.
        SmokeTester.Configure(options);
    }
}