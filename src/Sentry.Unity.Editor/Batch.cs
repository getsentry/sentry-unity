using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{

    public static class Batch
    {
        /// <summary>
        /// Allows the configuration of Sentry Options using Unity Batch.
        /// </summary>
        public static void ConfigureOptions() => ConfigureOptions(ParseCommandLineArguments());

        private static void ConfigureOptions(Dictionary<string, string> args, [CallerMemberName] string functionName = "")
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}: Invoking SentryOptions", functionName);

            if (!EditorApplication.ExecuteMenuItem("Tools/Sentry"))
            {
                throw new Exception("Menu item Tools -> Sentry was not found.");
            }

            var optionsWindow = EditorWindow.GetWindow<SentryWindow>();
            var options = optionsWindow.Options;

            if (options is null)
            {
                throw new InvalidOperationException("SentryOptions not found");
            }
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}: Found SentryOptions", functionName);

            var dsn = args["sentryOptions.Dsn"];
            if (dsn is { })
            {
                Debug.LogFormat(
                    LogType.Log, 
                    LogOption.NoStacktrace,
                    null, 
                    "{0}: Configuring DSN to {1}", functionName, dsn);

                options.Dsn = dsn;
            }

            optionsWindow.Close();
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "{0}: Sentry options Configured", functionName);
        }

        private static Dictionary<string, string> ParseCommandLineArguments()
        {
            var commandLineArguments = new Dictionary<string, string>();
            var args = Environment.GetCommandLineArgs();

            for (int current = 0, next = 1; current < args.Length; current++, next++)
            {
                if (!args[current].StartsWith("-"))
                {
                    continue;
                }

                var flag = args[current].TrimStart('-');
                var flagHasValue = next < args.Length && !args[next].StartsWith("-");
                var flagValue = flagHasValue ? args[next].TrimStart('-') : "";

                commandLineArguments.Add(flag, flagValue);
            }
            return commandLineArguments;
        }
    }
}