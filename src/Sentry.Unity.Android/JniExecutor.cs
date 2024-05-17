using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sentry.Unity.Android
{
    public class JniExecutor
    {
        private readonly AutoResetEvent _taskEvent;
        private readonly CancellationTokenSource _shutdownSource;
        private Delegate? _currentTask;

        private TaskCompletionSource<object?>? _taskCompletionSource;

        public JniExecutor()
        {
            _taskEvent = new AutoResetEvent(false);
            _shutdownSource = new CancellationTokenSource();

            new Thread(DoWork) { IsBackground = true }.Start();
        }

        private void DoWork()
        {
            AndroidJNI.AttachCurrentThread();

            while (!_shutdownSource.IsCancellationRequested)
            {
                _taskEvent.WaitOne();

                if (_currentTask is null)
                {
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
                    _taskCompletionSource?.SetException(e);
                }
                finally
                {
                    _currentTask = null;
                }
            }

            AndroidJNI.DetachCurrentThread();
        }

        public TResult? Run<TResult>(Func<TResult?> jniOperation)
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

        public void Run(Action jniOperation)
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

        public void Dispose()
        {
            _currentTask = null;
            _taskEvent.Set();
            _taskEvent.Dispose();
            _shutdownSource.Cancel();
        }
    }
}
