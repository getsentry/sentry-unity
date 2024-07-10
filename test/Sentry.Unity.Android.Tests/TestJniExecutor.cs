using System;

namespace Sentry.Unity.Android.Tests;

public class TestJniExecutor : IJniExecutor
{
    public TResult? Run<TResult>(Func<TResult?> jniOperation)
    {
        return default;
    }

    public void Run(Action jniOperation)
    {
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}