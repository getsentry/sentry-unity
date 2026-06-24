using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DependencyConflictPackage;
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
        Logger.Log("IntegrationTester, awake!");
        Application.quitting += () =>
        {
            Logger.Log("IntegrationTester is quitting.");
        };

        ExerciseConflictingDependencies();
    }

    // Invokes the DependencyConflict package, which ships plain, UNALIASED
    // System.*/Microsoft.* assemblies at versions that differ from the ones the
    // Sentry SDK ships aliased. Calling into it forces those assemblies to be
    // linked into the build right next to Sentry's aliased copies - so if the
    // assembly aliasing ever regresses, this build fails to compile/link rather
    // than the conflict going unnoticed.
    //
    // The "Dependencies say hi" / "FAILED" markers below are asserted by the
    // integration test harness (CommonTestCases.ps1), so a runtime conflict turns
    // the build red too instead of being swallowed into a log line.
    private void ExerciseConflictingDependencies()
    {
        try
        {
            var greeting = DependencyConflictPackageClient.SayHiAsync().GetAwaiter().GetResult();
            Logger.Log($"DependencyConflict: {greeting}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"DependencyConflict: FAILED - {ex}");
        }
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
            case "app-hang-capture":
                StartCoroutine(AppHangCapture());
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
        Logger.LogError("CRASH TEST: FAIL - unexpected code executed after crash");
        Application.Quit(1);
    }

    private IEnumerator AppHangCapture()
    {
        var hangId = Guid.NewGuid().ToString();

        AddIntegrationTestContext("app-hang-capture");

        // The native app-hang event is captured in-proc by sentry-native and its event ID is not
        // visible to C#. Tag the scope with a unique ID so the test harness can look the event up,
        // the same way crash-capture does (scope tags sync to the native layer).
        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("test.app_hang_id", hangId);
        });

        // Wait for the scope sync to complete and for the app-hang heartbeat coroutine to arm
        // (arming is deliberately deferred by a frame so startup isn't reported as a hang).
        yield return new WaitForSeconds(0.5f);

        Logger.Log($"EVENT_CAPTURED: {hangId}");
        Logger.Log("APP HANG TEST: Blocking the main thread to trigger native app-hang detection");

        // Block the main thread well past AppHangTimeout (2s in IntegrationOptionsConfiguration),
        // clearing the watchdog's 500ms poll and 1s heartbeat interval so detection reliably fires.
        System.Threading.Thread.Sleep(5000);

        // The main thread is responsive again; let the heartbeat resume and flush the captured event.
        yield return null;

        SentrySdk.FlushAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

        Logger.Log("APP HANG TEST: Flush complete, quitting.");
        Application.Quit(0);
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
