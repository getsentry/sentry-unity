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

    public static SentryCliOptions? LoadCliOptions() => LoadCliOptions(SentryCliOptions.GetConfigPath());

    internal static SentryCliOptions? LoadCliOptions(string path)
    {
        var savedCliOptions = Load<SentryCliOptions>(path);
        if (savedCliOptions == null)
        {
            return null;
        }

        // The configuration runs on a copy. The asset is the instance the Sentry window edits and saves, so values
        // set in code, such as an auth token read from the environment, would otherwise get written to disk.
        var cliOptions = Object.Instantiate(savedCliOptions);
        cliOptions.CliOptionsConfiguration?.Configure(cliOptions);

        return cliOptions;
    }

    public static SentryUnityOptions? LoadOptions(bool isBuilding = false)
    {
        var scriptableOptions = Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());
        return scriptableOptions?.ToSentryUnityOptions(isBuilding: isBuilding);
    }
}
