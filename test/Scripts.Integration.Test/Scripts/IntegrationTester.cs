using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Sentry;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.Diagnostics;

public class IntegrationTester : MonoBehaviour
{
    public void Start()
    {
        var arg = TestLauncher.GetTestArg();
        Debug.Log($"IntegrationTester arg: '{arg}'");

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
                Debug.LogError($"IntegrationTester: Unknown command: {arg}");
#if !UNITY_WEBGL
                Application.Quit(1);
#endif
                break;
        }
    }

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
        Debug.Log($"EVENT_CAPTURED: {eventId}");

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
            Debug.Log($"EVENT_CAPTURED: {eventId}");
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
        Debug.Log("INTEGRATION_TEST_COMPLETE");
#else
        Debug.Log("INTEGRATION_TEST_COMPLETE");
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

        Debug.Log($"EVENT_CAPTURED: {crashId}");
        Debug.Log("CRASH TEST: Issuing a native crash (AccessViolation)");

        Utils.ForceCrash(ForcedCrashCategory.AccessViolation);

        // Should not reach here
        Debug.LogError("CRASH TEST: FAIL - unexpected code executed after crash");
        Application.Quit(1);
    }

    private void CrashSend()
    {
        Debug.Log("CrashSend: Initializing Sentry to flush cached crash report...");

        var lastRunState = SentrySdk.GetLastRunState();
        Debug.Log($"CrashSend: crashedLastRun={lastRunState}");

        // Sentry is already initialized by IntegrationOptionsConfiguration.
        // Just wait a bit for the queued crash report to be sent, then quit.
        StartCoroutine(WaitAndQuit());
    }

    private IEnumerator WaitAndQuit()
    {
        // Wait for the crash report to be sent
        yield return new WaitForSeconds(10);

        SentrySdk.FlushAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();

        Debug.Log("CrashSend: Flush complete, quitting.");
        Application.Quit(0);
    }
}
