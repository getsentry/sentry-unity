using UnityEngine;

namespace Sentry.Unity
{
    public abstract class ScriptableOptionsConfiguration : ScriptableObject
    {
        /// <summary>
        /// Called during player build to after options are loaded from the asset.
        /// Changes made here will affect build-time processing, symbol upload, etc.
        /// Additionally, because iOS, macOS and Android native error handling is
        /// configured at build time, you can make changes to these options here.
        /// </summary>
        public abstract void ConfigureAtBuild(SentryUnityOptions options);

        /// <summary>
        /// Called at the player startup by SentryInitialization.
        /// You can alter configuration for the C# error handling and also
        /// native error handling in platforms other than iOS, macOS and Android.
        /// </summary>
        public abstract void ConfigureAtRuntime(SentryUnityOptions options);
    }
}
