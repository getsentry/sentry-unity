using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor : IJniExecutor
{
    private const int TimeoutMs = 16;

    private readonly CancellationTokenSource _shutdownSource;
    private readonly AutoResetEvent _taskEvent;
    private readonly IDiagnosticLogger? _logger;

    private Delegate _currentTask = null!; // The current task will always be set together with the task event

    private TaskCompletionSource<object?>? _taskCompletionSource;

    private readonly object _lock = new object();

    public JniExecutor(IDiagnosticLogger? logger)
    {
        _logger = logger;
        _taskEvent = new AutoResetEvent(false);
        _shutdownSource = new CancellationTokenSource();

        new Thread(DoWork) { IsBackground = true, Name = "SentryJniExecutorThread" }.Start();
    }

    private void DoWork()
    {
        AndroidJNI.AttachCurrentThread();

        var waitHandles = new[] { _taskEvent, _shutdownSource.Token.WaitHandle };

        while (true)
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

    public TResult? Run<TResult>(Func<TResult?> jniOperation)
    {
        lock (_lock)
        {
            _taskCompletionSource = new TaskCompletionSource<object?>();
            _currentTask = jniOperation;
            _taskEvent.Set();

            try
            {
                // if (!_taskCompletionSource.Task.Wait(TimeoutMs))
                // {
                //     throw new TimeoutException($"JNI operation timed out after {TimeoutMs}ms");
                // }
                // return (TResult?)_taskCompletionSource.Task.Result;
                return (TResult?)_taskCompletionSource.Task.GetAwaiter().GetResult();
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

    public void Run(Action jniOperation)
    {
        lock (_lock)
        {
            _taskCompletionSource = new TaskCompletionSource<object?>();
            _currentTask = jniOperation;
            _taskEvent.Set();

            try
            {
                // if (!_taskCompletionSource.Task.Wait(TimeoutMs))
                // {
                //     throw new TimeoutException($"JNI operation timed out after {TimeoutMs}ms");
                // }
                _taskCompletionSource.Task.Wait();
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
        _shutdownSource.Cancel();
        _taskEvent.Dispose();
    }
}
