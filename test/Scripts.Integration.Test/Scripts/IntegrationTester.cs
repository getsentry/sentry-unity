using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sentry;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.Diagnostics;

#if UNITY_WEBGL
using System.Web;
#endif

public class IntegrationTester : MonoBehaviour
{
    private void Awake()
    {
#if UNITY_GAMECORE
        // On Xbox, Debug.Log output is suppressed in non-development (master) builds.
        // Write to a file so the test harness can retrieve the output via xbcopy.
        //
        // Candidate paths are ordered by likelihood of working on a retail devkit:
        //   1. D:\Logs\ — known to be xbcopy-accessible, other apps (SentryPlayground) write here
        //   2. persistentDataPath — may resolve to a sandbox path that doesn't exist in master builds
        //   3. temporaryCachePath — same concern as persistentDataPath
        //   4. D:\ root — crash dumps land here, so it's writable
        var logFileName = "unity-integration-test.log";
        string persistentPath = null;
        string tempCachePath = null;
        try { persistentPath = Application.persistentDataPath; } catch { /* may throw on some configs */ }
        try { tempCachePath = Application.temporaryCachePath; } catch { /* may throw on some configs */ }

        var candidatePaths = new List<string>();
        candidatePaths.Add(@"D:\Logs\" + logFileName);
        if (!string.IsNullOrEmpty(persistentPath))
            candidatePaths.Add(Path.Combine(persistentPath, logFileName));
        if (!string.IsNullOrEmpty(tempCachePath))
            candidatePaths.Add(Path.Combine(tempCachePath, logFileName));
        candidatePaths.Add(@"D:\" + logFileName);

        string openedPath = null;
        string allErrors = "";
        foreach (var candidate in candidatePaths)
        {
            try
            {
                Logger.Open(candidate);
                openedPath = candidate;
                break;
            }
            catch (Exception ex)
            {
                allErrors += $"  {candidate}: {ex.GetType().Name}: {ex.Message}\n";
            }
        }

        if (openedPath != null)
        {
            Logger.Log($"Log file opened at: {openedPath}");
            Logger.Log($"persistentDataPath: {persistentPath ?? "(null)"}");
            Logger.Log($"temporaryCachePath: {tempCachePath ?? "(null)"}");

            // Write a breadcrumb file to D:\Logs so the test harness can discover where the log ended up.
            try
            {
                Directory.CreateDirectory(@"D:\Logs");
                File.WriteAllText(@"D:\Logs\unity-integration-test-path.txt", openedPath);
            }
            catch
            {
                // Best-effort — if D:\Logs isn't writable, the test harness will use candidate search.
            }
        }
        else
        {
            // None of the paths worked. Write diagnostics to D:\ before crashing so the test
            // harness can retrieve the file via xbcopy and see what went wrong.
            var diagMessage = $"Failed to open log file at any candidate path:\n{allErrors}";
            try
            {
                File.WriteAllText(@"D:\unity-integration-test-diag.txt", diagMessage);
            }
            catch
            {
                // If even D:\ root isn't writable, the crash dump is our only clue.
            }
            throw new IOException(diagMessage);
        }
#endif

        Logger.Log("IntegrationTester, awake!");
        Application.quitting += () =>
        {
            Logger.Log("IntegrationTester is quitting.");
        };
    }

    public void Start()
    {
        var arg = GetTestArg();
        Logger.Log($"IntegrationTester arg: '{arg}'");

        switch (arg)
        {
            case "message-capture":
                StartCoroutine(MessageCapture());
                break;
            case "exception-capture":
                StartCoroutine(ExceptionCapture());
                break;
            case "crash-capture":
                StartCoroutine(CrashCapture());
                break;
            case "crash-send":
                CrashSend();
                break;
            default:
                Logger.LogError($"IntegrationTester: Unknown command: {arg}");
#if !UNITY_WEBGL
                Application.Quit(1);
#endif
                break;
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    // .NET `Environment.GetCommandLineArgs()` doesn't seem to work on iOS so we get the test arg in Objective-C
    [DllImport("__Internal", EntryPoint="getTestArgObjectiveC")]
    private static extern string GetTestArg();
#else
    private static string GetTestArg()
    {
        string arg = null;
#if UNITY_EDITOR
#elif UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            arg = intent.Call<String>("getStringExtra", "test");
        }
#elif UNITY_WEBGL
        var uri = new Uri(Application.absoluteURL);
        arg = HttpUtility.ParseQueryString(uri.Query).Get("test");
#else
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 2 && args[1] == "--test")
        {
            arg = args[2];
        }
#endif
        return arg;
    }
#endif

    private void AddIntegrationTestContext(string testType)
    {
        SentrySdk.AddBreadcrumb("Integration test started");

        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("test.suite", "integration");
            scope.SetTag("test.type", testType);
            scope.User = new SentryUser
            {
                Id = "12345",
                Username = "TestUser",
                Email = "user-mail@test.abc"
            };
        });

        SentrySdk.AddBreadcrumb("Context configuration finished");
    }

    private IEnumerator MessageCapture()
    {
        AddIntegrationTestContext("message-capture");

        var eventId = SentrySdk.CaptureMessage("Integration test message");
        Logger.Log($"EVENT_CAPTURED: {eventId}");

        yield return CompleteAndQuit();
    }

    private IEnumerator ExceptionCapture()
    {
        AddIntegrationTestContext("exception-capture");

        try
        {
            DoSomeWork();
        }
        catch (Exception ex)
        {
            var eventId = SentrySdk.CaptureException(ex);
            Logger.Log($"EVENT_CAPTURED: {eventId}");
        }

        yield return CompleteAndQuit();
    }

    private IEnumerator CompleteAndQuit()
    {
#if UNITY_WEBGL
        // On WebGL, envelope sends are coroutine-based and need additional frames to
        // complete. Wait to avoid a race where the test harness shuts down the browser
        // before the send finishes.
        yield return new WaitForSeconds(3);
        Logger.Log("INTEGRATION_TEST_COMPLETE");
#else
        Logger.Log("INTEGRATION_TEST_COMPLETE");
        Application.Quit(0);
        yield break;
#endif
    }

    // Use a deeper call stack with NoInlining to ensure Unity 2022's IL2CPP
    // produces a non-empty managed stack trace (single-method throw/catch can
    // result in an empty stack trace with OptimizeSize + High stripping).
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DoSomeWork()
    {
        if (DateTime.Now.Ticks > 0) // Always true but not optimizable
        {
            ThrowException();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowException()
    {
        throw new InvalidOperationException("Integration test exception");
    }

    private IEnumerator CrashCapture()
    {
        var crashId = Guid.NewGuid().ToString();

        AddIntegrationTestContext("crash-capture");

        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("test.crash_id", crashId);
        });

        // Wait for the scope sync to complete on platforms that use a background thread (e.g. Android JNI)
        yield return new WaitForSeconds(0.5f);

        Logger.Log($"EVENT_CAPTURED: {crashId}");
        Logger.Log("CRASH TEST: Issuing a native crash (AccessViolation)");

        Utils.ForceCrash(ForcedCrashCategory.AccessViolation);

        // Should not reach here
        Logger.Log("ERROR: CRASH TEST: FAIL - unexpected code executed after crash");
        Application.Quit(1);
    }

    private void CrashSend()
    {
        Logger.Log("CrashSend: Initializing Sentry to flush cached crash report...");

        var lastRunState = SentrySdk.GetLastRunState();
        Logger.Log($"CrashSend: crashedLastRun={lastRunState}");

        // Sentry is already initialized by IntegrationOptionsConfiguration.
        // Just wait a bit for the queued crash report to be sent, then quit.
        StartCoroutine(WaitAndQuit());
    }

    private IEnumerator WaitAndQuit()
    {
        // Wait for the crash report to be sent
        yield return new WaitForSeconds(10);

        SentrySdk.FlushAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

        Logger.Log("CrashSend: Flush complete, quitting.");
        Application.Quit(0);
    }
}
