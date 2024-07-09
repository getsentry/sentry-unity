using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow;

public static class SentryEditorWindowInstrumentation
{
    /// <summary>
    /// Allows the configuration of Sentry Options using Unity Batch mode.
    /// </summary>
    public static void ConfigureOptions() => ConfigureOptions(CommandLineArgumentParser.Parse());

    private static void ConfigureOptions(Dictionary<string, string> args, [CallerMemberName] string functionName = "")
    {
        Debug.LogFormat("{0}: Invoking SentryOptions", functionName);

        if (!EditorApplication.ExecuteMenuItem(SentryWindow.EditorMenuPath.Replace(" -> ", "/")))
        {
            throw new Exception($"{functionName} failed: Menu item '{SentryWindow.EditorMenuPath}' not found. Was the Sentry UPM package installed?");
        }

        var optionsWindow = EditorWindow.GetWindow<SentryWindow>();
        var options = optionsWindow.Options;
        var cliOptions = optionsWindow.CliOptions;

        if (options is null || cliOptions is null)
        {
            throw new InvalidOperationException($"{functionName} failed: SentryOptions not found");
        }
        Debug.LogFormat("{0}: Found SentryOptions", functionName);

        var value = "";
        if (args.TryGetValue("runtimeOptionsScript", out value))
        {
            Debug.LogFormat("{0}: Configuring Runtime Options Script to {1}", functionName, value);
            OptionsConfigurationItem.SetScript(value);
        }

        if (args.TryGetValue("buildTimeOptionsScript", out value))
        {
            Debug.LogFormat("{0}: Configuring Build Time Options Script to {1}", functionName, value);
            OptionsConfigurationItem.SetScript(value);
        }

        if (args.TryGetValue("cliOptions.UrlOverride", out value))
        {
            Debug.LogFormat("{0}: Configuring symbol-upload UrlOverride to {1}", functionName, value);
            cliOptions.UrlOverride = value;
        }

        optionsWindow.Close();
        Debug.LogFormat("{0}: SUCCESS", functionName);
    }

    public static bool TryGetValue(this Dictionary<string, string> dict, String key, out bool value)
    {
        string strValue;
        value = false;
        if (!dict.TryGetValue(key, out strValue))
        {
            return false;
        }

        if (!Boolean.TryParse(strValue, out value))
        {
            throw new ArgumentException("Unknown boolean argument value: " + strValue, key);
        }
        return true;
    }
}