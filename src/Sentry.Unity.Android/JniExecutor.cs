using System;
using UnityEngine;

namespace Sentry.Unity.Android
{
    public class JniExecutor : IJniExecutor
    {
        public TResult? Run<TResult>(Func<TResult?> jniOperation)
        {
            AndroidJNI.AttachCurrentThread();
            return jniOperation();
        }

        public void Run(Action jniOperation)
        {
            AndroidJNI.AttachCurrentThread();
            jniOperation();
        }

        public void Dispose() { }
    }
}
