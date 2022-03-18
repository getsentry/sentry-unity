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

            if (options is null)
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

            optionsWindow.Close();
            Debug.LogFormat("{0}: Sentry options Configured", functionName);
        }
    }
}
