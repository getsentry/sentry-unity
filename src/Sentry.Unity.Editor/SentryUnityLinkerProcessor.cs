using System;
using Sentry.Extensibility;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.UnityLinker;

namespace Sentry.Unity.Editor;

internal sealed class SentryUnityLinkerProcessor : IUnityLinkerProcessor, IPostprocessBuildWithReport
{
    internal const string LinkSymbolsArgument = "--link-symbols";
    internal const string UnityLinkerAdditionalArgumentsEnvironmentVariable = "UNITYLINKER_ADDITIONAL_ARGS";

    public int callbackOrder => 0;

    public string GenerateAdditionalLinkXmlFile(BuildReport report, UnityLinkerBuildPipelineData data)
    {
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

        AddLinkSymbolsArgument(
            () => Environment.GetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable),
            arguments => Environment.SetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable, arguments),
            options.DiagnosticLogger);

        return string.Empty;
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        RemoveLinkSymbolsArgument(
            () => Environment.GetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable),
            arguments => Environment.SetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable, arguments));
    }

    internal static void AddLinkSymbolsArgument(
        Func<string?> getArguments,
        Action<string> setArguments,
        IDiagnosticLogger? logger = null)
    {
        var arguments = getArguments.Invoke();
        if (arguments?.Contains(LinkSymbolsArgument) == true)
        {
            logger?.LogDebug("Additional UnityLinker argument '{0}' already present.", LinkSymbolsArgument);
            return;
        }

        logger?.LogDebug("IL2CPP line number support enabled - Adding additional UnityLinker argument.");
        setArguments.Invoke(string.IsNullOrWhiteSpace(arguments)
            ? LinkSymbolsArgument
            : $"{arguments} {LinkSymbolsArgument}");
    }

    internal static void RemoveLinkSymbolsArgument(
        Func<string?> getArguments,
        Action<string> setArguments,
        IDiagnosticLogger? logger = null)
    {
        var arguments = getArguments.Invoke();
        if (arguments?.Contains(LinkSymbolsArgument) != true)
        {
            return;
        }

        logger?.LogDebug("Removing additional UnityLinker argument '{0}'.", LinkSymbolsArgument);
        setArguments.Invoke(arguments.Replace(LinkSymbolsArgument, "").Trim());
    }
}
