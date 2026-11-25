using Sentry.Unity;

/// <summary>
/// The sample's options: SentryOptionConfiguration's, and what the catalogue's samples need on top of them.
/// </summary>
public class SampleOptionConfiguration : SentryOptionConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        base.Configure(options);

#if UNITY_IOS
        // Reports the app being ended by iOS, as the out-of-memory sample is, on the next launch. The option is off by
        // default in the SDK. The sample app turns it on for every iOS run, because the out-of-memory sample's result
        // needs it.
        options.IosWatchdogTerminationIntegrationEnabled = true;
#endif
    }
}
