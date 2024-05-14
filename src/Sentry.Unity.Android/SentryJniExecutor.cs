using System;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity.Android
{
    public static class SentryJniExecutor
    {
        public static TResult? Run<TResult>(Func<TResult?> jniOperation)
        {
            TResult? result = default;
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                if (AndroidJNI.AttachCurrentThread() != 0)
                {
                    exception = new InvalidOperationException("Failed to attach thread to JVM");
                    return;
                }

                try
                {
                    result = jniOperation();
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            thread.Start();
            thread.Join();

            if (exception is not null)
            {
                Debug.LogException(exception);
            }

            return result;
        }

        public static void Run(Action jniOperation)
        {
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                if (AndroidJNI.AttachCurrentThread() != 0)
                {
                    exception = new InvalidOperationException("Failed to attach thread to JVM");
                    return;
                }

                try
                {
                    jniOperation();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            thread.Start();
            thread.Join();

            if (exception is not null)
            {
                Debug.LogException(exception);
            }
        }
    }
}
