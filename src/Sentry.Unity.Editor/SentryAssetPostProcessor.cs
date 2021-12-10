using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    public class SentryAssetPostProcessor : AssetPostprocessor
    {
        private static ListRequest? _listRequest;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets,
            string[] movedAssets, string[] movedFromAssetPaths)
        {
            // TODO: maybe filter for Packages/io.sentry?
            var isPackageImport = importedAssets.Any(path => path.StartsWith("Packages/"));

            if (isPackageImport)
            {
                _listRequest = Client.List(true);
                EditorApplication.update += UpdateListRequest;
            }
        }

        [MenuItem("Tools/TempPackageImport")]
        static void TempPackageImport()
        {
            _listRequest = Client.List(true);
            EditorApplication.update += UpdateListRequest;
        }

        private static void UpdateListRequest()
        {
            if (_listRequest is null)
            {
                EditorApplication.update -= UpdateListRequest;
                return;
            }

            if (!_listRequest.IsCompleted)
            {
                return;
            }

            if (_listRequest.Status == StatusCode.Success)
            {
                foreach (var package in _listRequest.Result)
                {
                    if (package.name == SentryPackageInfo.GetName())
                    {
                        Debug.Log("Found the Sentry package!");
                        // TODO: here we create the link.xml and the options and whatever else
                    }
                }
            }
            else
            {
            }

            EditorApplication.update -= UpdateListRequest;
        }
    }
}
