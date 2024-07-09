using UnityEngine;

namespace Sentry.Unity;

public abstract class SentryRuntimeOptionsConfiguration : ScriptableObject
{
    /// <summary>
    /// Called at the player startup by SentryInitialization.
    /// You can alter configuration for the C# error handling and also
    /// native error handling in platforms **other** than iOS, macOS and Android.
    /// </summary>
    /// <seealso cref="SentryBuildTimeOptionsConfiguration"/>
    public abstract void Configure(SentryUnityOptions options);
}
