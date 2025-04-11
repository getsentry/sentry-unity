using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor : IJniExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);
    
    private readonly ConcurrentQueue<JniOperation> _operationQueue = new ConcurrentQueue<JniOperation>();
    private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);
    private readonly IDiagnosticLogger? _logger;
    private readonly CancellationTokenSource _shutdownSource = new CancellationTokenSource();

    private Thread? _workerThread;
    private bool _isDisposed;

    private class JniOperation
    {
        public Delegate Action { get; }
        public TaskCompletionSource<object?>? CompletionSource { get; }

        public JniOperation(Delegate action, TaskCompletionSource<object?>? completionSource = null)
        {
            Action = action;
            CompletionSource = completionSource;
        }
    }

    public JniExecutor(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _workerThread = new Thread(ProcessJniQueue) { IsBackground = true, Name = "SentryJniExecutorThread" };
        _workerThread.Start();
    }

    private void ProcessJniQueue()
    {
        AndroidJNI.AttachCurrentThread();

        var waitHandles = new[] { _queueSignal, _shutdownSource.Token.WaitHandle };

        while (!_isDisposed)
        {
            var index = WaitHandle.WaitAny(waitHandles);
            if (index > 0 || _isDisposed)
            {
                break;
            }

            while (!_isDisposed && _operationQueue.TryDequeue(out var operation))
            {
                try
                {
                    ExecuteJniOperation(operation);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing JNI operation");
                    operation.CompletionSource?.TrySetException(ex);
                }
            }
        }

        AndroidJNI.DetachCurrentThread();
        _logger?.LogDebug("JNI executor thread terminated");
    }

    private void ExecuteJniOperation(JniOperation operation)
    {
        try
        {
            switch (operation.Action)
            {
                case Action action:
                    action.Invoke();
                    operation.CompletionSource?.TrySetResult(null);
                    break;
                case Func<bool> func1:
                    var result1 = func1.Invoke();
                    operation.CompletionSource?.TrySetResult(result1);
                    break;
                case Func<bool?> func2:
                    var result2 = func2.Invoke();
                    operation.CompletionSource?.TrySetResult(result2);
                    break;
                case Func<string?> func3:
                    var result3 = func3.Invoke();
                    operation.CompletionSource?.TrySetResult(result3);
                    break;
                default:
                    var error = new NotImplementedException($"Task type '{operation.Action.GetType()}' is not implemented in JniExecutor");
                    _logger?.LogError(error, "Unsupported JNI operation type");
                    operation.CompletionSource?.TrySetException(error);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during JNI operation execution");
            operation.CompletionSource?.TrySetException(ex);
        }
    }

    /// <summary>
    /// Runs a JNI operation that returns a result, waiting for completion with a timeout
    /// </summary>
    public TResult? Run<TResult>(Func<TResult?> jniOperation, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var completionSource = new TaskCompletionSource<object?>();
        var operation = new JniOperation(jniOperation, completionSource);
        
        _operationQueue.Enqueue(operation);
        _queueSignal.Set();

        try
        {
            if (completionSource.Task.Wait(timeout.Value))
            {
                return (TResult?)completionSource.Task.Result;
            }
            
            _logger?.LogError("JNI operation timed out after {0}ms", timeout.Value.TotalMilliseconds);
            return default;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error waiting for JNI operation result");
            return default;
        }
    }

    /// <summary>
    /// Runs a JNI operation with no return value, waiting for completion with a timeout
    /// </summary>
    public void Run(Action jniOperation, TimeSpan? timeout = null)
    {
        timeout ??= DefaultTimeout;
        var completionSource = new TaskCompletionSource<object?>();
        var operation = new JniOperation(jniOperation, completionSource);
        
        _operationQueue.Enqueue(operation);
        _queueSignal.Set();

        try
        {
            if (!completionSource.Task.Wait(timeout.Value))
            {
                _logger?.LogError("JNI operation timed out after {0}ms", timeout.Value.TotalMilliseconds);
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error waiting for JNI operation completion");
        }
    }

    /// <summary>
    /// Runs a JNI operation without waiting for completion - true fire and forget
    /// </summary>
    public void RunAsync(Action jniOperation)
    {
        var operation = new JniOperation(jniOperation);
        _operationQueue.Enqueue(operation);
        _queueSignal.Set();
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _shutdownSource.Cancel();
        
        try
        {
            _workerThread?.Join(100);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error during JNI executor thread shutdown");
        }

        _queueSignal.Dispose();
        _shutdownSource.Dispose();
    }
}
