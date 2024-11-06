using Sentry;
using Sentry.Unity;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        // Here you can programmatically modify the Sentry option properties used for the SDK's initialization

        options.Environment = "dev";

        options.NativeOverrides.Environment = "native_dev";
        options.NativeOverrides.SampleRate = 0.1f;
    }
}
