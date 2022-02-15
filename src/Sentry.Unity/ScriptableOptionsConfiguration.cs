using UnityEngine;

namespace Sentry.Unity
{
    public abstract class ScriptableOptionsConfiguration : ScriptableObject
    {
        public abstract void Configure(SentryUnityOptions options);
    }
}
