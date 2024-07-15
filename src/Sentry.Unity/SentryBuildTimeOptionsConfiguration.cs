using UnityEngine;

namespace Sentry.Unity;

public abstract class SentryBuildTimeOptionsConfiguration : ScriptableObject
{
    /// <summary>
    /// Called during app build. Changes made here will affect build-time processing, symbol upload, etc.
    /// Additionally, because iOS, macOS and Android native error handling is configured at build time,
    /// you can make changes to these options here.
    /// </summary>
    /// <seealso cref="SentryRuntimeOptionsConfiguration"/>
    public abstract void Configure(SentryUnityOptions options, SentryCliOptions cliOptions);
}
