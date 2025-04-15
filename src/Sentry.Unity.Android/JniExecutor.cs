using System;
using Sentry.Extensibility;

namespace Sentry.Unity.Android;

internal class JniExecutor
{
    private readonly IDiagnosticLogger? _logger;
    private readonly IAndroidJNI _androidJNI;

    public JniExecutor(IDiagnosticLogger? logger, IAndroidJNI? androidJNI = null)
    {
        _logger = logger;
        _androidJNI ??= androidJNI ?? AndroidJNIAdapter.Instance;
    }

    public TResult? Run<TResult>(Func<TResult?> jniOperation, bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is not true)
        {
            _androidJNI.AttachCurrentThread();
        }

        try
        {
            return jniOperation.Invoke();
        }
        finally
        {
            if (isMainThread is not true)
            {
                _androidJNI.DetachCurrentThread();
            }
        }
    }

    public void Run(Action jniOperation, bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();
        if (isMainThread is not true)
        {
            _androidJNI.AttachCurrentThread();
        }

        try
        {
            jniOperation.Invoke();
        }
        finally
        {
            if (isMainThread is not true)
            {
                _androidJNI.DetachCurrentThread();
            }
        }
    }
}
