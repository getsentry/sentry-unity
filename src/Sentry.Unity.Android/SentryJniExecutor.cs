using System;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity.Android
{
    internal static class SentryJniExecutor
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
                // Adding the Sentry logger tag ensures we don't send this error to Sentry.
                Debug.unityLogger.Log(LogType.Exception, UnityLogger.LogTag, $"Error during JNI execution: {exception}");
            }

            return result;
        }

        public static void FireAndForget(Action jniOperation)
        {
            Exception? exception = null;

            new Thread(() =>
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
            }).Start();

            if (exception is not null)
            {
                // Adding the Sentry logger tag ensures we don't send this error to Sentry.
                Debug.unityLogger.Log(LogType.Exception, UnityLogger.LogTag, $"Error during JNI execution: {exception}");
            }
        }
    }
}
