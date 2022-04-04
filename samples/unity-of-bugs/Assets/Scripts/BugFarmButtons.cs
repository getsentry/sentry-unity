using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class BugFarmButtons : MonoBehaviour
{
    private Action<Action> _executor;

    private void Start()
    {
        Debug.Log("Sample Start 🦋");
        Debug.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
        OnThreadingChange(0);
    }

    public void OnThreadingChange(int value)
    {
        string name;
        switch (value)
        {
            case 0:
                name = "Main (UI) Thread";
                _executor = (Action fn) => fn();
                break;
            case 1:
                name = "Background: Task (unawaited)";
                _executor = (Action fn) => Task.Run(fn);
                break;
            case 2:
                name = "Background: Task (awaited)";
                _executor = async (Action fn) => await Task.Run(fn);
                break;
            case 3:
                name = "Background: Coroutine";
                _executor = (Action fn) => StartCoroutine(Coroutine(fn));
                break;
            default:
                throw new ArgumentException($"Invalid threading dropdown value: {value}");

        }
        Debug.LogFormat("Setting BugFarm implementation to: {0} = {1}", value, name);
    }

    public IEnumerator Coroutine(Action fn)
    {
        yield return null;
        fn();
    }

    public void AssertFalse() => _executor(() => Assert.AreEqual(true, false));

    public void ThrowNull() => _executor(() => throw null);

    public void ThrowExceptionAndCatch() => _executor(() =>
    {
        Debug.Log("Throwing an instance of 🐛 CustomException!");

        try
        {
            throw new CustomException("Custom bugs 🐛🐛🐛🐛.");
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    });

    public void ThrowNullAndCatch() => _executor(() =>
    {
        Debug.Log("Throwing 'null' and catching 🐜🐜🐜 it!");

        try
        {
            ThrowNull();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    });

    public void CaptureMessage() => _executor(() => SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️"));

    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from A lady beetle 🐞");

    public void StackTraceExampleA() => _executor(() => StackTraceExampleB());
}

public class CustomException : Exception
{
    public CustomException(string message) : base(message)
    {
    }
}
