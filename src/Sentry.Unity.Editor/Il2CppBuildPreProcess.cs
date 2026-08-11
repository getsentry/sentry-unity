using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Sentry.Unity.Editor;

internal class Il2CppBuildPreProcess : IPreprocessBuildWithReport
{
    internal const string SourceMappingArgument = "--emit-source-mapping";
    private static IDiagnosticLogger? Logger;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
        if (PlayerSettings.GetScriptingBackend(namedBuildTarget) != ScriptingImplementation.IL2CPP)
        {
            return;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        if (options is null)
        {
            return;
        }

        Logger = options.DiagnosticLogger;
        Logger?.LogInfo("IL2CPP build detected. Handling additional IL2CPP arguments.");

        SetAdditionalIl2CppArguments(options,
            PlayerSettings.GetAdditionalIl2CppArgs,
            PlayerSettings.SetAdditionalIl2CppArgs);
    }

    internal static void SetAdditionalIl2CppArguments(SentryUnityOptions options, Func<string> getArguments, Action<string> setArguments)
    {
        if (options.Il2CppLineNumberSupportEnabled)
        {
            Logger?.LogDebug("IL2CPP line number support enabled - Adding additional IL2CPP arguments.");

            var arguments = getArguments.Invoke();
            if (arguments.Contains(SourceMappingArgument))
            {
                Logger?.LogDebug("Additional argument '{0}' already present.", SourceMappingArgument);
                return;
            }

            setArguments.Invoke(getArguments.Invoke() + $" {SourceMappingArgument}");
        }
        else
        {
            var arguments = getArguments.Invoke();
            if (arguments.Contains(SourceMappingArgument))
            {
                Logger?.LogDebug("IL2CPP line number support disabled - Removing additional IL2CPP arguments.");

                arguments = arguments.Replace(SourceMappingArgument, "");
                setArguments.Invoke(arguments);
            }
        }
    }
}
