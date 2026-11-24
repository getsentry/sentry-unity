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
        // Reports the app being ended by iOS, as the out-of-memory sample is, on the next launch.
        options.IosWatchdogTerminationIntegrationEnabled = true;
#endif
    }
}
