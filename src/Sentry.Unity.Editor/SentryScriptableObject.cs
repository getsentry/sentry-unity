using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal static class SentryScriptableObject
    {
        internal static T Load<T>(string path) where T : ScriptableObject
        {
            Debug.Log($"loading from: {path}");
            var options = AssetDatabase.LoadAssetAtPath<T>(path);
            if (options == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                options = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(options, path);
                AssetDatabase.SaveAssets();
            }

            return options;
        }
    }
}
