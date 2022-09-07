using UnityEditor;
using UnityEngine;

public class SentryPackageManagerExtension
{
    [InitializeOnLoadMethod]
    public static void PackageManagerThing()
    {
        Debug.Log("Adding 'Sentry Package Manager Extension'");
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
