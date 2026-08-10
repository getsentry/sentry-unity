using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

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

public sealed class SentryUnityLinkerProcessor : IUnityLinkerProcessor, IPostprocessBuildWithReport
{
    internal const string LinkSymbolsArgument = "--link-symbols";
    internal const string UnityLinkerAdditionalArgumentsEnvironmentVariable = "UNITYLINKER_ADDITIONAL_ARGS";
    private static string? UnityLinkerArgumentsBeforeBuild;
    private static bool UnityLinkerArgumentsChanged;

    public int callbackOrder => 0;

    public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
    {
        if (UnityLinkerArgumentsChanged)
        {
            return string.Empty;
        }

        var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(report.summary.platformGroup);
        if (PlayerSettings.GetScriptingBackend(namedBuildTarget) != ScriptingImplementation.IL2CPP)
        {
            return string.Empty;
        }

        var options = SentryScriptableObject.LoadOptions(isBuilding: true);
        if (options is null || !options.Il2CppLineNumberSupportEnabled)
        {
            return string.Empty;
        }

        var argumentsBeforeBuild = Environment.GetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable);
        if (AddLinkSymbolsArgument(
                () => argumentsBeforeBuild,
                arguments => Environment.SetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable, arguments),
                options.DiagnosticLogger))
        {
            UnityLinkerArgumentsBeforeBuild = argumentsBeforeBuild;
            UnityLinkerArgumentsChanged = true;
        }

        return string.Empty;
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        if (!UnityLinkerArgumentsChanged)
        {
            return;
        }

        Environment.SetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable, UnityLinkerArgumentsBeforeBuild);
        UnityLinkerArgumentsBeforeBuild = null;
        UnityLinkerArgumentsChanged = false;
    }

    internal static bool AddLinkSymbolsArgument(
        Func<string?> getArguments,
        Action<string> setArguments,
        IDiagnosticLogger? logger = null)
    {
        var arguments = getArguments.Invoke();
        if (arguments?.IndexOf(LinkSymbolsArgument, StringComparison.Ordinal) >= 0)
        {
            logger?.LogDebug("Additional UnityLinker argument '{0}' already present.", LinkSymbolsArgument);
            return false;
        }

        logger?.LogDebug("IL2CPP line number support enabled - Adding additional UnityLinker argument.");
        setArguments.Invoke(string.IsNullOrWhiteSpace(arguments)
            ? LinkSymbolsArgument
            : $"{arguments} {LinkSymbolsArgument}");
        return true;
    }
}
