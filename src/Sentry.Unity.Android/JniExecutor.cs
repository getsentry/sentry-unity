using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor
{
    private readonly IDiagnosticLogger? _logger;

    public JniExecutor(IDiagnosticLogger? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Runs a JNI operation that returns a result, waiting for completion with a timeout
    /// </summary>
    public TResult? Run<TResult>(Func<TResult?> jniOperation)
    {
        if (SentryUnityVersion.IsNewerOrEqualThan("2020"))
        {
            if (!MainThreadData.IsMainThread())
            {
                AndroidJNI.AttachCurrentThread();
            }

            try
            {
                return jniOperation.Invoke();
            }
            finally
            {
                if (!MainThreadData.IsMainThread())
                {
                    AndroidJNI.DetachCurrentThread();
                }
            }
        }
        else
        {
            AndroidJNI.AttachCurrentThread();
            try
            {
                return jniOperation.Invoke();
            }
            finally
            {
                AndroidJNI.DetachCurrentThread();
            }
        }
    }

    /// <summary>
    /// Runs a JNI operation with no return value, waiting for completion with a timeout
    /// </summary>
    public void Run(Action jniOperation)
    {
        if (SentryUnityVersion.IsNewerOrEqualThan("2020"))
        {
            if (!MainThreadData.IsMainThread())
            {
                AndroidJNI.AttachCurrentThread();
            }

            try
            {
                jniOperation.Invoke();
            }
            finally
            {
                if (!MainThreadData.IsMainThread())
                {
                    AndroidJNI.DetachCurrentThread();
                }
            }
        }
        else
        {
            AndroidJNI.AttachCurrentThread();
            try
            {
                jniOperation.Invoke();
            }
            finally
            {
                AndroidJNI.DetachCurrentThread();
            }
        }
    }
}
