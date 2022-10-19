using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor.ConfigurationWindow
{
    public static class SentryEditorWindowInstrumentation
    {
        /// <summary>
        /// Allows the configuration of Sentry Options using Unity Batch mode.
        /// </summary>
        public static void ConfigureOptions() => ConfigureOptions(CommandLineArgumentParser.Parse());

        private static void ConfigureOptions(Dictionary<string, string> args, [CallerMemberName] string functionName = "")
        {
            Debug.LogFormat("{0}: Invoking SentryOptions", functionName);

            if (!EditorApplication.ExecuteMenuItem("Tools/Sentry"))
            {
                throw new Exception($"{functionName} failed: Menu item 'Tools -> Sentry' not found. Was the Sentry UPM package installed?");
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
            bool boolValue;
            if (args.TryGetValue("sentryOptions.Dsn", out value))
            {
                Debug.LogFormat("{0}: Configuring DSN to {1}", functionName, value);
                options.Dsn = value;
            }

            if (args.TryGetValue("sentryOptionsScript", out value))
            {
                Debug.LogFormat("{0}: Configuring Options Script to {1}", functionName, value);
                OptionsConfigurationDotNet.SetScript(value);
            }

            if (args.TryGetValue("attachScreenshot", out boolValue))
            {
                Debug.LogFormat("{0}: Configuring AttachScreenshot to {1}", functionName, boolValue);
                options.AttachScreenshot = boolValue;
            }

            if (args.TryGetValue("diagnosticLevel", out value))
            {
                Debug.LogFormat("{0}: Configuring DiagnosticLevel to {1}", functionName, value);
                options.DebugOnlyInEditor = false;
                options.DiagnosticLevel = value switch
                {
                    "debug" => SentryLevel.Debug,
                    "info" => SentryLevel.Info,
                    "warning" => SentryLevel.Warning,
                    "error" => SentryLevel.Error,
                    "fatal" => SentryLevel.Fatal,
                    _ => throw new ArgumentException($"Invalid DiagnosticLevel value: {value}")
                };
            }

            if (args.TryGetValue("il2cppLineNumbers", out boolValue))
            {
                Debug.LogFormat("{0}: Configuring Il2CppLineNumberSupportEnabled to {1}", functionName, boolValue);
                options.Il2CppLineNumberSupportEnabled = boolValue;
            }

            if (args.TryGetValue("cliOptions.Org", out value))
            {
                Debug.LogFormat("{0}: Configuring symbol-upload organization to {1}", functionName, value);
                cliOptions.Organization = value;
            }

            if (args.TryGetValue("cliOptions.Project", out value))
            {
                Debug.LogFormat("{0}: Configuring symbol-upload project to {1}", functionName, value);
                cliOptions.Project = value;
            }

            if (args.TryGetValue("cliOptions.Auth", out value))
            {
                Debug.LogFormat("{0}: Configuring symbol-upload auth token", functionName);
                cliOptions.Auth = value;
            }

            if (args.TryGetValue("cliOptions.UrlOverride", out value))
            {
                Debug.LogFormat("{0}: Configuring symbol-upload UrlOverride to {1}", functionName, value);
                cliOptions.UrlOverride = value;
            }

            if (args.TryGetValue("cliOptions.UploadSources", out boolValue))
            {
                Debug.LogFormat("{0}: Configuring symbol-upload UploadSources to {1}", functionName, boolValue);
                cliOptions.UploadSources = boolValue;
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
}
