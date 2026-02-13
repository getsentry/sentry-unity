using System;
using System.Collections;
using System.Collections.Generic;
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
            throw new InvalidOperationException("Integration test exception");
        }
        catch (Exception ex)
        {
            var eventId = SentrySdk.CaptureException(ex);
            Debug.Log($"EVENT_CAPTURED: {eventId}");
        }

        Application.Quit(0);
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
        Debug.Log("CRASH TEST: Issuing a native crash (FatalError)");

        Utils.ForceCrash(ForcedCrashCategory.FatalError);

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
