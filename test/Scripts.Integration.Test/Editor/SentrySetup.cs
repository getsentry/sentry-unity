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

            LogDebug($"Installing Sentry from {installOrigin}");
            var installCommand = GetInstallCommand(installOrigin);
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

    static string GetInstallCommand(SentryInstallOrigin origin)
    {
        if (origin == SentryInstallOrigin.Disk)
        {
            var sentryPackageLocal = "file:" + Application.dataPath.Replace("samples/IntegrationTest/Assets", "test-package-release/");
            LogDebug("Sentry package Path is " + sentryPackageLocal);
            return sentryPackageLocal;
        }
        var errorMessage = $"Install command {origin} not supported";
        LogError(errorMessage);
        EditorApplication.Exit(-1);
        // Throw since we will not return any value here.
        throw new NotImplementedException(errorMessage);
    }

    static SentryInstallOrigin GetInstallOriginFromEnvironment(Dictionary<string, string> args)
    {
        if (args.ContainsKey("installSentry") && Enum.TryParse(args["installSentry"], out SentryInstallOrigin origin))
        {
            return origin;
        }
        return SentryInstallOrigin.None;
    }
}
