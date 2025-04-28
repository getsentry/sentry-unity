using Sentry.Integrations;

namespace Sentry.Unity.Integrations;

public class SentryUnityAssetStoreIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (options is SentryUnityOptions unityOptions)
        {
            unityOptions.SdkIntegrationNames.Add("IL2CPPLineNumbers");
        }
    }
}
