using System;

namespace Sentry.Unity.Android
{
    internal interface IJniExecutor : IDisposable
    {
        public TResult? Run<TResult>(Func<TResult?> jniOperation);
        public void Run(Action jniOperation);
    }
}
