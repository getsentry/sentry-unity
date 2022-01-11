using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class Builder
{
    public static void BuildIl2CPPPlayer(BuildTarget buildTarget)
    {
        var args = ParseCommandLineArguments();
        ValidateArguments(args);

        if (args.ContainsKey("sentryOptions.configure"))
        {
            SetupSentryOptions(args);
        }

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = new[] {"Assets/Scenes/1_Bugfarm.unity"},
            locationPathName = args["buildPath"],
            target = buildTarget,
            options = BuildOptions.StrictMode,
        };

        var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        var summary = report.summary;

        Debug.Log("Build result at outputPath: " + report.summary.outputPath);

        switch (summary.result)
        {
            case BuildResult.Succeeded:
                Debug.Log($"Build succeeded: {summary.totalSize} bytes");
                break;
            default:
                var message = $"Build result: {summary.result} with {summary.totalErrors}" +
                              $" error{(summary.totalErrors > 1 ? "s" : "")}.";

                Debug.Log(message);
                throw new Exception(message);
        }

        if (summary.totalErrors > 0)
        {
            var message = $"Build succeeded with {summary.totalErrors} error{(summary.totalErrors > 1 ? "s" : "")}.";
            Debug.Log(message);
            // Break the build
            throw new Exception(message);
        }

        if (summary.totalWarnings > 0)
        {
            Debug.Log($"Build succeeded with {summary.totalWarnings} warning{(summary.totalWarnings > 1 ? "s" : "")}.");
        }
    }

    public static void BuildWindowsIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneWindows64);
    public static void BuildMacIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.StandaloneOSX);
    public static void BuildAndroidIl2CPPPlayer() => BuildIl2CPPPlayer(BuildTarget.Android);
    public static void BuildIOSPlayer() => BuildIl2CPPPlayer(BuildTarget.iOS);

    private static void SetupSentryOptions(Dictionary<string, string> args)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "SetupSentryOptions: Invoking SentryOptions");

        if (!EditorApplication.ExecuteMenuItem("Tools/Sentry"))
        {
            throw new Exception("SetupSentryOptions: Menu item Tools -> Sentry was not found.");
        }

        var sentryWindowType = AppDomain.CurrentDomain.GetAssemblies()
            ?.FirstOrDefault(assembly => assembly.FullName.StartsWith("Sentry.Unity.Editor,"))
            ?.GetTypes()?.FirstOrDefault(type => type.FullName == "Sentry.Unity.Editor.SentryWindow");
        if (sentryWindowType is null)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies().Where(name => name.FullName.Contains("Sentry")))
            {
                foreach(var type in asm.GetTypes())
                {
                    Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "SetupSentryOptions: Asm {0} Type {1}", asm.FullName, type.FullName);
                }
            }
            throw new EntryPointNotFoundException("SetupSentryOptions: Type SentryWindow not found");
        }

        var optionsWindow = EditorWindow.GetWindow(sentryWindowType);
        var options = optionsWindow.GetType().GetProperty("Options").GetValue(optionsWindow);

        if (options is null)
        {
            throw new EntryPointNotFoundException("SetupSentryOptions: Method SentryOptions not found");
        }
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "SetupSentryOptions: Found SentryOptions");

        var dsn = args["sentryOptions.Dsn"];
        if (dsn != null)
        {
            Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null,
                "SetupSentryOptions: Configuring Dsn to {0}", dsn);

            var dnsPropertyInfo = options.GetType().GetProperty("Dsn");
            dnsPropertyInfo.SetValue(options, dsn, null);
        }

        optionsWindow.Close();
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "SetupSentryOptions: Sentry options Configured");
    }

    public static Dictionary<string, string> ParseCommandLineArguments()
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

    private static void ValidateArguments(Dictionary<string, string> args)
    {
        if (!args.ContainsKey("buildPath") || string.IsNullOrWhiteSpace(args["buildPath"]))
        {
            throw new Exception("No valid '-buildPath' has been provided.");
        }
    }
}
