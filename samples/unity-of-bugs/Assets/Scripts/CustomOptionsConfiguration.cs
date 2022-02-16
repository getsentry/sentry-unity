using Sentry.Unity;
using UnityEngine;

[CreateAssetMenu(fileName = "Assets/Resources/Sentry/CustomOptionsConfiguration", menuName = "Sentry/CustomOptionsConfiguration", order = 999)]
public class CustomOptionsConfiguration : ScriptableOptionsConfiguration
{
    // This method gets called when you instantiated the scriptable object and added it to the configuration window
    public override void Configure(SentryUnityOptions options)
    {
        // NOTE: You have complete access to the options object here but changes to the options will not make it
        // to the native layer because the native layer is configured during build time.

        options.BeforeSend = sentryEvent =>
        {
            if (sentryEvent.Tags.ContainsKey("SomeTag"))
            {
                // Don't send events with a tag SomeTag to Sentry
                return null;
            }

            return sentryEvent;
        };
    }
}
