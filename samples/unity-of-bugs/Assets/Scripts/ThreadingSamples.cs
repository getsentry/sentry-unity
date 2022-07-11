using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using UnityEngine;
using UnityEngine.Assertions;

public class ThreadingSamples : MonoBehaviour
{
    private Action<Action> _executor;

    private void Start()
    {
        Debug.Log("Threading Samples 🧵");
        OnThreadingChange(0);
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

    public void AssertFalse() => _executor(() => Assert.AreEqual(true, false));

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ThrowNull() => _executor(() => throw null);

    public void ThrowExceptionAndCatch() => _executor(() =>
    {
        Debug.Log("Throwing an instance of 🐛🧵🐛 CustomException!");

        try
        {
            throw new CustomException("Custom bugs 🐛🧵🐛.");
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    });

    public void ThrowNullAndCatch() => _executor(() =>
    {
        Debug.Log("Throwing 'null' and catching 🐜🧵🐜 it!");

        try
        {
            ThrowNull();
        }
        catch (Exception e)
        {
            SentrySdk.CaptureException(e);
        }
    });

    public void CaptureMessage() => _executor(() => SentrySdk.CaptureMessage("🕷️🧵️🕷️ Spider message 🕷️🧵️🕷️"));

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void StackTraceExampleB() => throw new InvalidOperationException("Exception from a lady beetle 🐞🧵");

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void StackTraceExampleA() => _executor(() => StackTraceExampleB());

    public void LogError() => _executor(() => Debug.LogError("Debug.LogError() called"));
}
