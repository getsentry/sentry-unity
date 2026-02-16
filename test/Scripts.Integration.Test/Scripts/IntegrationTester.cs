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
                MessageCapture();
                break;
            case "exception-capture":
                ExceptionCapture();
                break;
            case "crash-capture":
                CrashCapture();
                break;
            case "crash-send":
                CrashSend();
                break;
            default:
                Debug.LogError($"IntegrationTester: Unknown command: {arg}");
                Application.Quit(1);
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

    private void MessageCapture()
    {
        AddIntegrationTestContext("message-capture");

        var eventId = SentrySdk.CaptureMessage("Integration test message");
        Debug.Log($"EVENT_CAPTURED: {eventId}");

        Application.Quit(0);
    }

    private void ExceptionCapture()
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

        Application.Quit(0);
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

    private void CrashCapture()
    {
        var crashId = Guid.NewGuid().ToString();

        AddIntegrationTestContext("crash-capture");

        SentrySdk.ConfigureScope(scope =>
        {
            scope.SetTag("test.crash_id", crashId);
        });

        Debug.Log($"EVENT_CAPTURED: {crashId}");
        Debug.Log("CRASH TEST: Issuing a native crash (Abort)");

        Utils.ForceCrash(ForcedCrashCategory.Abort);

        // Should not reach here
        Debug.LogError("CRASH TEST: FAIL - unexpected code executed after crash");
        Application.Quit(1);
    }

    private void CrashSend()
    {
        Debug.Log("CrashSend: Initializing Sentry to flush cached crash report...");

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
