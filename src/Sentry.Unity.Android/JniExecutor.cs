using System;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;

namespace Sentry.Unity.Android;

internal class JniExecutor
{
    private readonly IDiagnosticLogger? _logger;
    private readonly IAndroidJNI _androidJNI;
    private readonly IApplication _application;

    public JniExecutor(IDiagnosticLogger? logger, IAndroidJNI? androidJNI = null, IApplication? application = null)
    {
        _logger = logger;
        _androidJNI ??= androidJNI ?? AndroidJNIAdapter.Instance;
        _application ??= application ?? ApplicationAdapter.Instance;
    }

    /// <summary>
    /// Runs a JNI operation that returns a result, waiting for completion with a timeout
    /// </summary>
    public TResult? Run<TResult>(Func<TResult?> jniOperation, bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();

        _logger?.LogDebug("Checking for version");
        if (SentryUnityVersion.IsNewerOrEqualThan("2020.3", _application))
        {
            _logger?.LogDebug("Is newer");
            if (isMainThread is not true)
            {
                _logger?.LogDebug("is non main thread");
                _androidJNI.AttachCurrentThread();
            }

            try
            {
                _logger?.LogDebug("invoking");
                return jniOperation.Invoke();
            }
            finally
            {
                _logger?.LogDebug("finally");
                if (isMainThread is not true)
                {
                    _logger?.LogDebug("still not main thread");
                    _androidJNI.DetachCurrentThread();
                }
            }
        }
        else
        {
            _logger?.LogDebug("else part");
            _androidJNI.AttachCurrentThread();
            try
            {
                _logger?.LogDebug("invoking");
                return jniOperation.Invoke();
            }
            finally
            {
                _logger?.LogDebug("finally");
                _androidJNI.DetachCurrentThread();
            }
        }
    }

    /// <summary>
    /// Runs a JNI operation with no return value, waiting for completion with a timeout
    /// </summary>
    public void Run(Action jniOperation, bool? isMainThread = null)
    {
        isMainThread ??= MainThreadData.IsMainThread();

        _logger?.LogDebug("Checking for version");
        if (SentryUnityVersion.IsNewerOrEqualThan("2020.3", _application))
        {
            _logger?.LogDebug("Is newer");
            if (isMainThread is not true)
            {
                _logger?.LogDebug("is non main thread");
                _androidJNI.AttachCurrentThread();
            }

            try
            {
                _logger?.LogDebug("invoking");
                jniOperation.Invoke();
            }
            finally
            {
                _logger?.LogDebug("finally");
                if (isMainThread is not true)
                {
                    _logger?.LogDebug("still not main thread");
                    _androidJNI.DetachCurrentThread();
                }
            }
        }
        else
        {
            _logger?.LogDebug("else part");
            _androidJNI.AttachCurrentThread();
            try
            {
                _logger?.LogDebug("invoking");
                jniOperation.Invoke();
            }
            finally
            {
                _logger?.LogDebug("finally");
                _androidJNI.DetachCurrentThread();
            }
        }
    }
}
