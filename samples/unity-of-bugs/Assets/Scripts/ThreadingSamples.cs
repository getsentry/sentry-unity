using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Unity;
using UnityEngine;

public class ThreadingSamples : MonoBehaviour
{
    private Action<Action> _executor;

    private void Start()
    {
        Debug.Log("Threading Samples 🧵");
        OnThreadingChange(0);
    }

    public void ThrowUnhandledException()
    {
        _executor(() =>
        {
            Debug.Log("Throwing a unhandled 🕷 exception!");
            DoSomeWorkHere();
        });
    }

    public void ThrowExceptionButCatch()
    {
        _executor(() =>
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
        });
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
        _executor(() =>
        {
            if (CheckSomeFakeWork())
            {
                // Messages do not have a stacktrace attached by default. This is an opt-in feature.
                // Note: That stack traces generated for message events are provided without line numbers. See known limitations
                // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
                SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
            }
        });
    }

    public void LogError()
    {
        _executor(() =>
        {
            if (CheckSomeFakeWork())
            {
                // Error logs get captured as messages and do not have a stacktrace attached by default. This is an opt-in feature.
                // Note: That stack traces generated for message events are provided without line numbers. See known limitations
                // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
                Debug.LogError("Debug.LogError() called");
            }
        });
    }

    public void LogException()
    {
        _executor(() =>
        {
            if (CheckSomeFakeWork())
            {
                // Error logs get captured as messages and do not have a stacktrace attached by default. This is an opt-in feature.
                Debug.LogException(new NullReferenceException("Some bugs are harder to catch than others. 🦋"));
            }
        });
    }

    public void OnThreadingChange(int value)
    {
        string name;
        switch (value)
        {
            case 0:
                name = "Main (UI) Thread";
                _executor = fn => fn();
                break;
            case 1:
                name = "Background: Task";
                _executor = fn => Task.Run(fn);
                break;
            case 2:
                name = "Background: Task (awaited)";
                _executor = async fn => await Task.Run(fn);
                break;
            case 3:
                name = "Background: Coroutine";
                _executor = fn => StartCoroutine(Coroutine(fn));
                break;
            case 4:
                name = "Background: Thread";
                _executor = fn => new Thread(() => fn()).Start();
                break;
            default:
                throw new ArgumentException($"Invalid threading dropdown value: {value}");

        }

        Debug.Log($"Setting execution to: '{name}'");
    }

    public IEnumerator Coroutine(Action fn)
    {
        yield return null;
        fn();
    }

    // 'NoInlining' ends up being inlined through L2CPP anyway. :(
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
