using System;

namespace Sentry.Unity.Android
{
    public interface IJniExecutor : IDisposable
    {
        public TResult? Run<TResult>(Func<TResult?> jniOperation);
        public void Run(Action jniOperation);
    }
}
