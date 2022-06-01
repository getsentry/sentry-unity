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

            if (args.TryGetValue("attachScreenshot", out value))
            {
                bool boolValue;
                if (!Boolean.TryParse(value, out boolValue))
                {
                    throw new ArgumentException("Unknown boolean argument value: " + value, "attachScreenshot");
                }
                Debug.LogFormat("{0}: Configuring AttachScreenshot to {1}", functionName, boolValue);
                options.AttachScreenshot = boolValue;
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
            optionsWindow.Close();
            Debug.LogFormat("{0}: Sentry options Configured", functionName);
        }
    }
}
