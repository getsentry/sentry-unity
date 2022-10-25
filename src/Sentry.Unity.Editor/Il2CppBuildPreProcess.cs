using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal class Il2CppBuildPreProcess : IPreprocessBuildWithReport
    {
        internal const string SourceMappingArgument = "--emit-source-mapping";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (PlayerSettings.GetScriptingBackend(report.summary.platformGroup) != ScriptingImplementation.IL2CPP)
            {
                return;
            }


            var options = SentryScriptableObject.LoadOptions()?.ToSentryUnityOptions(true);

            if (options is null)
            {
                return;
            }

            SetAdditionalIl2CppArguments(options,
                PlayerSettings.GetAdditionalIl2CppArgs,
                PlayerSettings.SetAdditionalIl2CppArgs);
        }

        internal static void SetAdditionalIl2CppArguments(SentryUnityOptions options, Func<string> getArguments, Action<string> setArguments)
        {
            if (options.Il2CppLineNumberSupportEnabled)
            {
                options.DiagnosticLogger?.LogDebug("IL2CPP line number support enabled - Adding additional IL2CPP arguments.");

                var arguments = getArguments.Invoke();
                if (arguments.Contains(SourceMappingArgument))
                {
                    options.DiagnosticLogger?.LogDebug("Additional argument '{0}' already present.", SourceMappingArgument);
                    return;
                }

                setArguments.Invoke(getArguments.Invoke() + $" {SourceMappingArgument}");
            }
            else
            {
                var arguments = getArguments.Invoke();
                if (arguments.Contains(SourceMappingArgument))
                {
                    options.DiagnosticLogger?.LogDebug("IL2CPP line number support disabled - Removing additional IL2CPP arguments.");

                    arguments = arguments.Replace(SourceMappingArgument, "");
                    setArguments.Invoke(arguments);
                }
            }
        }
    }
}
