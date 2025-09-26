using System.IO;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor;

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

    public static SentryCliOptions? LoadCliOptions()
    {
        var cliOptions = Load<SentryCliOptions>(SentryCliOptions.GetConfigPath());
        cliOptions?.CliOptionsConfiguration?.Configure(cliOptions);

        return cliOptions;
    }

    public static SentryUnityOptions? LoadOptions(bool isBuilding = false)
    {
        var scriptableOptions = Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
        return scriptableOptions?.ToSentryUnityOptions(isBuilding: isBuilding);
    }
}
