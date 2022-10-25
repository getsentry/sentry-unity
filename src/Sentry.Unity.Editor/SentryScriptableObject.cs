using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal static class SentryScriptableObject
    {
        internal static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var options = Load<T>(path);
            if (options == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                options = ScriptableObject.CreateInstance<T>();

                AssetDatabase.CreateAsset(options, path);
                AssetDatabase.SaveAssets();
            }

            return options;
        }

        internal static T? Load<T>(string path) where T : ScriptableObject => AssetDatabase.LoadAssetAtPath<T>(path);

        internal static SentryCliOptions? LoadCliOptions() => Load<SentryCliOptions>(SentryCliOptions.GetConfigPath());
        internal static ScriptableSentryUnityOptions? LoadOptions() =>
            Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
    }
}
