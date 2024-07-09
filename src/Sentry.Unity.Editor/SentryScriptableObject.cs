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

    private static SentryCliOptions? LoadCliOptions() => Load<SentryCliOptions>(SentryCliOptions.GetConfigPath());
    internal static ScriptableSentryUnityOptions? LoadOptions() =>
        Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath());

    internal static (SentryUnityOptions?, SentryCliOptions?) ConfiguredBuildTimeOptions()
    {
        var scriptableOptions = LoadOptions();
        var cliOptions = LoadCliOptions();

        SentryUnityOptions? options = null;
        if (scriptableOptions is not null)
        {
            options = scriptableOptions.ToSentryUnityOptions(isBuilding: true, unityInfo: null);
            // Must be non-nullable in the interface otherwise Unity script compilation fails...
            cliOptions ??= ScriptableObject.CreateInstance<SentryCliOptions>();
            var setupScript = scriptableOptions.BuildTimeOptionsConfiguration;
            if (setupScript != null)
            {
                setupScript.Configure(options, cliOptions);
            }
        }

        return (options, cliOptions);
    }
}