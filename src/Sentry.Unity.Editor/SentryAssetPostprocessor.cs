using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var imported = importedAssets.Any(path => path.StartsWith("Packages/"));
            var deleted = deletedAssets.Any(path => path.StartsWith("Packages/"));

            if (imported || deleted)
            {
                InitializeOnLoad();
            }
        }

        /**
         * Instead of hooking into AssetPostprocessing we could InitializeOnLoad
         * But this gets called on domain reload (i.e. enter playmode) and could be disabled by the user.
         */
        // [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            // ASYNC! Fetching all packages the current project depends on
            var listRequest = Client.List(true);
            while (!listRequest.IsCompleted)
            {
                Thread.Sleep(100);
            }

            if (listRequest.Error != null)
            {
                Debug.Log("Error: " + listRequest.Error.message);
                return;
            }

            var packages = listRequest.Result;
            foreach (var package in packages)
            {
                if (package.name.StartsWith("io.sentry"))
                {
                    SentryGettingStartedWindow.OpenWindow();
                }
            }
        }
    }
}
