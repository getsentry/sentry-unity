using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor : IJniExecutor
{
    // We're capping out at 16ms - 1 frame at 60 frames per second
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly CancellationTokenSource _shutdownSource;
    private readonly AutoResetEvent _taskEvent;
    private readonly IDiagnosticLogger? _logger;

    private Delegate _currentTask = null!; // The current task will always be set together with the task event

    private TaskCompletionSource<object?>? _taskCompletionSource;

    private readonly object _lock = new object();

    private bool _isDisposed;
    private Thread? _workerThread;

    public JniExecutor(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _taskEvent = new AutoResetEvent(false);
        _shutdownSource = new CancellationTokenSource();

        _workerThread = new Thread(DoWork) { IsBackground = true, Name = "SentryJniExecutorThread" };
        _workerThread.Start();
    }

    private void DoWork()
    {
        AndroidJNI.AttachCurrentThread();

        var waitHandles = new[] { _taskEvent, _shutdownSource.Token.WaitHandle };

        while (!_isDisposed)
        {
            var index = WaitHandle.WaitAny(waitHandles);
            if (index > 0)
            {
                // We only care about the _taskEvent
                break;
            }

            try
            {
                // Matching the type of the `_currentTask` exactly. The result gets cast to the expected type
                // when returning from the blocking call.
                switch (_currentTask)
                {
                    case Action action:
                        {
                            action.Invoke();
                            _taskCompletionSource?.SetResult(null);
                            break;
                        }
                    case Func<bool> func1:
                        {
                            var result = func1.Invoke();
                            _taskCompletionSource?.SetResult(result);
                            break;
                        }
                    case Func<bool?> func2:
                        {
                            var result = func2.Invoke();
                            _taskCompletionSource?.SetResult(result);
                            break;
                        }
                    case Func<string?> func3:
                        {
                            var result = func3.Invoke();
                            _taskCompletionSource?.SetResult(result);
                            break;
                        }
                    default:
                        throw new NotImplementedException($"Task type '{_currentTask?.GetType()}' with value '{_currentTask}' is not implemented in the JniExecutor.");
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error during JNI execution.");
                _taskCompletionSource?.SetException(e);
            }
        }

        AndroidJNI.DetachCurrentThread();
    }

    public TResult? Run<TResult>(Func<TResult?> jniOperation, TimeSpan? timeout = null)
    {
        lock (_lock)
        {
            timeout ??= DefaultTimeout;
            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            _taskCompletionSource = new TaskCompletionSource<object?>();
            _currentTask = jniOperation;
            _taskEvent.Set();

            try
            {
                _taskCompletionSource.Task.Wait(timeoutCts.Token);
                return (TResult?)_taskCompletionSource.Task.Result;
            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("JNI execution timed out.");
                return default;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error during JNI execution.");
                return default;
            }
            finally
            {
                _currentTask = null!;
            }
        }
    }

    public void Run(Action jniOperation, TimeSpan? timeout = null)
    {
        lock (_lock)
        {
            timeout ??= DefaultTimeout;
            using var timeoutCts = new CancellationTokenSource(timeout.Value);
            _taskCompletionSource = new TaskCompletionSource<object?>();
            _currentTask = jniOperation;
            _taskEvent.Set();

            try
            {
                _taskCompletionSource.Task.Wait(timeoutCts.Token);
            }
            catch (OperationCanceledException)
            {
                _logger?.LogError("JNI execution timed out.");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "Error during JNI execution.");
            }
            finally
            {
                _currentTask = null!;
            }
        }
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
        catch (ThreadStateException)
        {
            _logger?.LogError("JNI Executor Worker thread was never started during disposal");
        }
        catch (ThreadInterruptedException)
        {
            _logger?.LogError("JNI Executor Worker thread was interrupted during disposal");
        }

        _taskEvent.Dispose();
    }
}
