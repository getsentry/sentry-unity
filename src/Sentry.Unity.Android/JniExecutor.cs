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

    public TResult? Run<TResult>(Func<TResult?> jniOperation) => Run(jniOperation, MainThreadData.IsMainThread());

    // Internal for testing
    internal TResult? Run<TResult>(Func<TResult?> jniOperation, bool isMainThread)
    {
        if (isMainThread is false)
        {
            _androidJNI.AttachCurrentThread();
        }

        try
        {
            return jniOperation.Invoke();
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to execute operation");
        }
        finally
        {
            if (isMainThread is false)
            {
                _androidJNI.DetachCurrentThread();
            }
        }

        return default;
    }

    public void Run(Action jniOperation) => Run(jniOperation, MainThreadData.IsMainThread());

    // Internal for testing
    internal void Run(Action jniOperation, bool isMainThread)
    {
        if (isMainThread is false)
        {
            _androidJNI.AttachCurrentThread();
        }

        try
        {
            jniOperation.Invoke();
        }
        finally
        {
            if (isMainThread is false)
            {
                _androidJNI.DetachCurrentThread();
            }
        }
    }
}
