using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(3);

    private JniOperation? _highPriorityOperation;
    private static readonly object HighPriorityOperationLock = new();

    private readonly ConcurrentQueue<JniOperation> _lowPriorityQueue = new();
    private const int LowPriorityQueueCapacity = 100;
    private int _lowPriorityQueueSize;

    private readonly IDiagnosticLogger? _logger;

    private readonly AutoResetEvent _workerSignal = new(false);
    private readonly CancellationTokenSource _shutdownSource = new();

    private readonly Thread? _workerThread;
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

        var waitHandles = new[] { _workerSignal, _shutdownSource.Token.WaitHandle };

        while (!_isDisposed)
        {
            if (_highPriorityOperation is not null)
            {
                JniOperation? highPriorityOperation;
                lock (HighPriorityOperationLock)
                {
                    highPriorityOperation = _highPriorityOperation;
                    _highPriorityOperation = null;
                }

                ExecuteJniOperation(highPriorityOperation);
                continue;
            }

            if (_lowPriorityQueue.TryDequeue(out var lowPriorityOp))
            {
                Interlocked.Decrement(ref _lowPriorityQueueSize);
                ExecuteJniOperation(lowPriorityOp);
                continue;
            }

            WaitHandle.WaitAny(waitHandles);
            if (_shutdownSource.IsCancellationRequested || _isDisposed)
            {
                break;
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
            _logger?.LogError(ex, "Error during JNI operation execution.");
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

        lock (HighPriorityOperationLock)
        {
            _highPriorityOperation = operation;
            _workerSignal.Set();
        }

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

        lock (HighPriorityOperationLock)
        {
            _highPriorityOperation = operation;
            _workerSignal.Set();
        }


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
        if (!TryEnqueueOperation(operation))
        {
            // _logger?.LogWarning("Low priority JNI operation queue is full ({0}/{1}). Operation rejected.",
            //     _lowPriorityQueueSize, LowPriorityQueueCapacity);
        }
    }

    private bool TryEnqueueOperation(JniOperation operation)
    {
        if (Interlocked.Increment(ref _lowPriorityQueueSize) <= LowPriorityQueueCapacity)
        {
            _lowPriorityQueue.Enqueue(operation);
            _workerSignal.Set();
            return true;
        }

        Interlocked.Decrement(ref _lowPriorityQueueSize);
        return false;
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

        _workerSignal.Dispose();
        _shutdownSource.Dispose();
    }
}
