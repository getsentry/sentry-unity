#if UNITY_2020_3_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public static class SentryPackageManager
    {
        [InitializeOnLoadMethod]
        public static void SentryPackageUpdate()
        {
            UnityEditor.PackageManager.Events.registeredPackages += eventArgs =>
            {
                if (eventArgs.changedFrom.Count > 0)
                {
                    var from = eventArgs.changedFrom[0];
                    var to = eventArgs.changedTo[0];

                    Debug.Log($"Package changed from {from.name}-{from.version} to {to.name}-{to.version}");
                }
            };
        }
    }
}
#endif
