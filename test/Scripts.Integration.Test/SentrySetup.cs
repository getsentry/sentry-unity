using UnityEditor;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public static class Startup
{
    static AddRequest AddRequest;
    static ListRequest ListRequest;

    const string SentryPackageName = "io.sentry.unity";
    const string SentryUPMUrl = "https://github.com/getsentry/unity.git";

    static void LogDebug(string message)
        => Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, $"Sentry setup: {message}");

    static void LogError(string message)
        => Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, $"Sentry setup: {message}");

    static void ExitIfBatchMode(int code)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(code);
        }
    }
    static Startup()
    {
        LogDebug("checking if Sentry is installed");
        throw new System.Exception("I am an error :D ");
        CheckIfSentryIsInstalled();
    }

    static void RequestSentryInstall()
    {
        AddRequest = Client.Add(SentryUPMUrl);
        EditorApplication.update += SentrySetupProgress;
    }

    static void CheckIfSentryIsInstalled()
    {
        ListRequest = Client.List();
        EditorApplication.update += SentryIsInstalledProgress;
    }

    static void SentrySetupProgress()
    {
        if (AddRequest.IsCompleted)
        {
            EditorApplication.update -= SentrySetupProgress;

            if (AddRequest.Status == StatusCode.Success)
            {
                LogDebug("SUCCESS");
                ExitIfBatchMode(0);
            }
            else if (AddRequest.Status >= StatusCode.Failure)
            {
                LogError("FAILED");
                LogError(AddRequest.Error.message);
                ExitIfBatchMode(-1);
            }
        }
    }

    static void SentryIsInstalledProgress()
    {
        if (ListRequest.IsCompleted)
        {
            EditorApplication.update -= SentryIsInstalledProgress;

            if (ListRequest.Status >= StatusCode.Failure)
            {
                LogError(ListRequest.Error.message);
            }
            else if (ListRequest.Result.Any(p => p.name.Contains(SentryPackageName)) == false)
            {
                LogDebug("Sentry not found, installing.");
                RequestSentryInstall();
            }
            else
            {
                LogDebug("already installed.");
                ExitIfBatchMode(0);
            }
        }
    }
}
