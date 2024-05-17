using System;
using UnityEngine;

namespace Sentry.Unity.Android
{
    public class JniExecutor : IJniExecutor
    {
        public TResult? Run<TResult>(Func<TResult?> jniOperation)
        {
            try
            {
                AndroidJNI.AttachCurrentThread();
                return jniOperation();
            }
            finally
            {
                AndroidJNI.DetachCurrentThread();
            }
        }

        public void Run(Action jniOperation)
        {
            AndroidJNI.AttachCurrentThread();
            jniOperation();
            AndroidJNI.DetachCurrentThread();
        }

        public void Dispose() { }
    }
}
