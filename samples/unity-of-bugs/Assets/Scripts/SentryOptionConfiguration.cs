using Sentry;
using Sentry.Unity;
using UnityEngine;

public class SentryOptionConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
#if UNITY_ANDROID
        options.SampleRate = 0.1f;
#elif UNITY_IOS
        options.SampleRate = 1.0f;
#endif
    }
}
