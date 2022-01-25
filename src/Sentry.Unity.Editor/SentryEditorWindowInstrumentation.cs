using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;

namespace Sentry.Unity.Editor
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
                throw new Exception("Menu item 'Tools -> Sentry' not found. Was the Sentry UPM package installed?");
            }

            var optionsWindow = EditorWindow.GetWindow<SentryWindow>();
            var options = optionsWindow.Options;

            if (options is null)
            {
                throw new InvalidOperationException("SentryOptions not found");
            }
            Debug.LogFormat("{0}: Found SentryOptions", functionName);

            var dsn = args["sentryOptions.Dsn"];
            if (dsn is { })
            {
                Debug.LogFormat("{0}: Configuring DSN to {1}", functionName, dsn);
                options.Dsn = dsn;
            }

            optionsWindow.Close();
            Debug.LogFormat("{0}: Sentry options Configured", functionName);
        }
    }
}
