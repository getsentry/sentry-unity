using System;
using UnityEditor;

namespace Sentry.Unity.Editor;

/// <summary>
/// The UnityLinker picks additional arguments up from the environment. Unity spawns the Bee backend that ultimately
/// invokes the linker as a child process, so the environment has to be set before that happens. Build callbacks run
/// too close to the backend getting spawned to reliably win that race, which is why this runs on editor startup and
/// on every domain reload instead.
/// </summary>
[InitializeOnLoad]
internal static class SentryUnityLinkerArguments
{
    internal const string LinkSymbolsArgument = "--link-symbols";
    internal const string UnityLinkerAdditionalArgumentsEnvironmentVariable = "UNITYLINKER_ADDITIONAL_ARGS";

    static SentryUnityLinkerArguments() => AddLinkSymbolsArgument(
        () => Environment.GetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable),
        arguments => Environment.SetEnvironmentVariable(UnityLinkerAdditionalArgumentsEnvironmentVariable, arguments));

    internal static void AddLinkSymbolsArgument(Func<string?> getArguments, Action<string> setArguments)
    {
        var arguments = getArguments.Invoke();
        if (arguments?.Contains(LinkSymbolsArgument) == true)
        {
            return;
        }

        setArguments.Invoke(string.IsNullOrWhiteSpace(arguments)
            ? LinkSymbolsArgument
            : $"{arguments} {LinkSymbolsArgument}");
    }
}
