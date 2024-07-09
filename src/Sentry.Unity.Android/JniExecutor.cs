using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class JniExecutor : IJniExecutor
{
    private readonly CancellationTokenSource _shutdownSource;
    private readonly AutoResetEvent _taskEvent;
    private Delegate _currentTask = null!; // The current task will always be set together with the task event

    private TaskCompletionSource<object?>? _taskCompletionSource;

    private readonly object _lock = new object();

    public JniExecutor()
    {
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
                        case Func<bool?> func1:
                            {
                                var result = func1.Invoke();
                                _taskCompletionSource?.SetResult(result);
                                break;
                            }
                        case Func<string?> func2:
                            {
                                var result = func2.Invoke();
                                _taskCompletionSource?.SetResult(result);
                                break;
                            }
                        default:
                            throw new ArgumentException("Invalid type for _currentTask.");
                    }
                }
                catch (Exception e)
                {
                    Debug.unityLogger.Log(LogType.Exception, UnityLogger.LogTag, $"Error during JNI execution: {e}");
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
                    return (TResult?)_taskCompletionSource.Task.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    Debug.unityLogger.Log(LogType.Exception, UnityLogger.LogTag, $"Error during JNI execution: {e}");
                }

                return default;
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
                    _taskCompletionSource.Task.Wait();
                }
                catch (Exception e)
                {
                    Debug.unityLogger.Log(LogType.Exception, UnityLogger.LogTag, $"Error during JNI execution: {e}");
                }
            }
        }

    public void Dispose()
    {
            _shutdownSource.Cancel();
            _taskEvent.Dispose();
        }
}