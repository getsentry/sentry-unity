using Sentry;
using UnityEngine;
using Sentry.Unity;
using UnityEngine.SceneManagement;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/RuntimeConfiguration.asset", menuName = "Sentry/RuntimeConfiguration", order = 999)]
public class RuntimeConfiguration : SentryRuntimeOptionsConfiguration
{
    /// Called at the player startup by SentryInitialization.
    /// You can alter configuration for the C# error handling and also
    /// native error handling in platforms **other** than iOS, macOS and Android.
    /// Learn more at https://docs.sentry.io/platforms/unity/configuration/options/#programmatic-configuration
    public override void Configure(SentryUnityOptions options)
    {
        // Note that changes to the options here will **not** affect iOS, macOS and Android events. (i.e. environment and release)
        // Take a look at `SentryBuildTimeOptionsConfiguration` instead.

        Debug.Log(nameof(RuntimeConfiguration) + "::Configure() called");

        // Making sure the SDK is not already initialized during tests
        var sceneName = SceneManager.GetActiveScene().name;
        if (sceneName != null && sceneName.Contains("TestScene"))
        {
            Debug.Log("Disabling the SDK while running tests.");
            options.Enabled = false;
        }

        // BeforeSend is only relevant at runtime. It wouldn't hurt to be set at build time, just wouldn't do anything.
        options.SetBeforeSend((sentryEvent, _) =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        });

        Debug.Log(nameof(RuntimeConfiguration) + "::Configure() finished");
    }
}
