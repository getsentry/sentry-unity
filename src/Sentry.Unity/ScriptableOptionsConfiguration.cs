using UnityEngine;

namespace Sentry.Unity
{
    public abstract class ScriptableOptionsConfiguration : ScriptableObject
    {
        /// <summary>
        /// Called at the player startup by SentryInitialization.
        /// You can alter configuration for the C# error handling and also
        /// native error handling in platforms other than iOS, macOS and Android.
        /// </summary>
        /// <seealso cref="Sentry.Unity.Editor.ScriptableOptionsConfiguration"/>
        public abstract void Configure(SentryUnityOptions options);
    }
}
