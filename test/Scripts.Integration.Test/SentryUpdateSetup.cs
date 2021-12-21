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
    static string SentryPackageLocalPath;

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
        var sentryPackageLocal = "file:" + Application.dataPath.Replace("samples/IntegrationTest/Assets", "package-release/");
        LogDebug("Sentry package Path is " + sentryPackageLocal);
        SentryPackageLocalPath = sentryPackageLocal;
        LogDebug("checking if sentry is installed");
        RequestSentryInstall();
    }

    static void RequestSentryInstall()
    {
        AddRequest = Client.Add(SentryPackageLocalPath + "wrong-path");
        EditorApplication.update += SentrySetupProgress;
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
}
