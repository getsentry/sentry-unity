using System;
using System.Runtime.CompilerServices;
using Sentry.Unity;
using UnityEngine;

public class BugFarmButtons : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("The 🐛s awaken!");
    }

    private void Start()
    {
        // Log messages are getting captured as breadcrumbs
        Debug.Log("Starting the 🦋-Farm");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
    }

    public void ThrowUnhandledException()
    {
        Debug.Log("Throwing an unhandled 🕷 exception!");
        DoSomeWorkHere();
    }

    public void ThrowExceptionButCatch()
    {
        Debug.Log("Throwing an exception but catching it! 🐜");

        try
        {
            DoSomeWorkHere();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    }

    private void DoSomeWorkHere()
    {
        if (CheckSomeFakeWork())
        {
            DoSomeWorkThere();
        }
    }

    private void DoSomeWorkThere()
    {
        if (CheckSomeFakeWork())
        {
            throw new CustomException("Exception from an exceptional lady beetle 🐞!");
        }
    }

    public void CaptureMessage()
    {
        if (CheckSomeFakeWork())
        {
            // Messages do not have a stacktrace attached by default. This is an opt-in feature.
            // Note: That stack traces generated for message events are provided without line numbers. See known limitations
            // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
            SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
        }
    }

    public void LogError()
    {
        if (CheckSomeFakeWork())
        {
            // Error logs get captured as messages and do not have a stacktrace attached by default. This is an opt-in feature.
            // Note: That stack traces generated for message events are provided without line numbers. See known limitations
            // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
            Debug.LogError("Debug.LogError() called");
        }
    }

    public void LogException()
    {
        if (CheckSomeFakeWork())
        {
            // Error logs get captured as messages and do not have a stacktrace attached by default. This is an opt-in feature.
            Debug.LogException(new NullReferenceException("Some bugs are harder to catch than others. 🦋"));
        }
    }

    // NoInlining ends up being inlined through L2CPP anyway. :(
    // We're checking some fake work here to prevent too aggressive optimization. That way, we can show off some proper
    // stack traces that are closer to real-world bugs and events.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool CheckSomeFakeWork() => DateTime.Now.Ticks > 0; // Always true but not optimizable

    private class CustomException : Exception
    {
        public CustomException(string message) : base(message)
        { }
    }
}
