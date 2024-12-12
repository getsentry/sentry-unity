using System;

namespace Sentry.Unity.Android.Tests;

public class TestJniExecutor : IJniExecutor
{
    public TResult? Run<TResult>(Func<TResult?> jniOperation, TimeSpan? timeout = null)
    {
        return default;
    }

    public void Run(Action jniOperation, TimeSpan? timeout = null)
    {
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}
