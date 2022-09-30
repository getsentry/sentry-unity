using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sentry.Unity.Editor
{
    internal class Il2CppOption : IPreprocessBuildWithReport
    {
        private const string SourceMappingArgument = "--emit-source-mapping";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (PlayerSettings.GetScriptingBackend(report.summary.platformGroup) != ScriptingImplementation.IL2CPP)
            {
                return;
            }

            var options = SentryScriptableObject
                .Load<ScriptableSentryUnityOptions>(ScriptableSentryUnityOptions.GetConfigPath())
                ?.ToSentryUnityOptions(BuildPipeline.isBuildingPlayer);

            if (options is null)
            {
                return;
            }

            if (options.Il2CppLineNumberSupportEnabled)
            {
                options.DiagnosticLogger?.LogDebug("IL2CPP line number support enabled - Adding additional IL2CPP arguments.");

                var arguments = PlayerSettings.GetAdditionalIl2CppArgs();
                if (arguments.Contains(SourceMappingArgument))
                {
                    options.DiagnosticLogger?.LogDebug("Additional argument '{0}' already present.", SourceMappingArgument);
                    return;
                }

                PlayerSettings.SetAdditionalIl2CppArgs(PlayerSettings.GetAdditionalIl2CppArgs() + $" {SourceMappingArgument}");
            }
            else
            {
                var arguments = PlayerSettings.GetAdditionalIl2CppArgs();
                if (arguments.Contains(SourceMappingArgument))
                {
                    options.DiagnosticLogger?.LogDebug("IL2CPP line number support disabled - Removing additional IL2CPP arguments.");

                    arguments = arguments.Remove(arguments.IndexOf(SourceMappingArgument, StringComparison.Ordinal),
                        SourceMappingArgument.Length);
                    PlayerSettings.SetAdditionalIl2CppArgs(arguments);
                }
            }

        }
    }
}
