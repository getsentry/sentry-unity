using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class CommandLineArguments
{
    public static Dictionary<string, string> Parse()
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
public class SentrySetup
{
    enum SentryInstallOrigin
    {
        None,
        Disk
    };

    static void LogDebug(string message)
        => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, "Sentry Package Installation: {0}", message);

    static void LogError(string message)
        => Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, "Sentry Package Installation: {0}", message);

    [InitializeOnLoadMethod]
    static void InstallSentry()
    {
        var installOrigin = GetInstallOriginFromEnvironment(CommandLineArguments.Parse());

        if (installOrigin == SentryInstallOrigin.None)
        {
            LogDebug("Sentry not requested to be installed.");
        }
        else
        {
            LogDebug("Checking if Sentry is installed");

            var listRequest = Client.List();
            while (!listRequest.IsCompleted)
            {
                Thread.Sleep(1000);
            }
            if (listRequest.Status >= StatusCode.Failure)
            {
                LogError(listRequest.Error.message);
                EditorApplication.Exit(-1);
            }
            else if (listRequest.Result.Any(p => p.name == "io.sentry.unity") == false)
            {
                LogDebug("Project does not contain Sentry.");
            }
            else
            {
                LogDebug("Project contains Sentry.");
            }

            var args = CommandLineArguments.Parse();
            LogDebug($"Installing Sentry from {installOrigin}");
            var installCommand = GetInstallCommand(installOrigin, args);
            var addRequest = Client.Add(installCommand);
            while (!addRequest.IsCompleted)
            {
                Thread.Sleep(1000);
            }
            if (addRequest.Status == StatusCode.Success)
            {
                LogDebug("SUCCESS");
                EditorApplication.Exit(0);
            }
            else
            {
                LogError("FAILED");
                LogError(addRequest.Error?.message);
                EditorApplication.Exit(-1);
            }
        }
    }

    static string GetInstallCommand(SentryInstallOrigin origin, Dictionary<string, string> args)
    {
        if (origin == SentryInstallOrigin.Disk)
        {
            if (!args.ContainsKey("sentryPackagePath"))
            {
                var errorMessage = "Disk install requires -sentryPackagePath argument";
                LogError(errorMessage);
                EditorApplication.Exit(-1);
                throw new ArgumentException(errorMessage);
            }
            var sentryPackageLocal = $"file:{args["sentryPackagePath"]}";
            LogDebug("Sentry package Path is " + sentryPackageLocal);
            return sentryPackageLocal;
        }
        var errorMessage2 = $"Install command {origin} not supported";
        LogError(errorMessage2);
        EditorApplication.Exit(-1);
        throw new NotImplementedException(errorMessage2);
    }

    static SentryInstallOrigin GetInstallOriginFromEnvironment(Dictionary<string, string> args)
    {
        return args.ContainsKey("installSentry")
            ? (SentryInstallOrigin)Enum.Parse(typeof(SentryInstallOrigin), args["installSentry"])
            : SentryInstallOrigin.None;
    }
}
