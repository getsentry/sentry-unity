using System;

namespace Sentry.Unity.Android;

internal interface IJniExecutor : IDisposable
{
    public TResult? Run<TResult>(Func<TResult?> jniOperation, TimeSpan? timeout = null);
    public void Run(Action jniOperation, TimeSpan? timeout = null);
}
