using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using UnityEngine;
using Timer = System.Timers.Timer;

namespace Sentry.Unity
{
    internal interface IUnityLogMessageFilter
    {
        // Attempt.1
        bool Debounced(LogType type, string condition);

        // Attempt.2
        bool DebouncedTime(LogType type, string condition);
    }

    internal sealed class TypeConditionFilter : IUnityLogMessageFilter, IDisposable
    {
        // Attempt.1
        private readonly HashSet<Key> _eventContainer;

        // Attempt.2
        private readonly Dictionary<Key, DateTimeOffset> _eventContainerTime;

        private readonly TimeSpan _barrierStep;
        private readonly Timer _timer;
        private DateTimeOffset _barrierOffset;

        /// <summary>
        /// Shortcut for UTC
        /// </summary>
        private static DateTimeOffset Now => DateTimeOffset.UtcNow;

        public TypeConditionFilter()
        {
            _eventContainerTime = new();
            _eventContainer = new();
            _barrierStep = TimeSpan.FromSeconds(2);
            _barrierOffset = Now.Add(_barrierStep);

            _timer = new Timer(2000)
            {
                AutoReset = true,
                Enabled = true
            };
            /*_timer.Elapsed += TimerElapsed;*/
        }

        /*
         * Experiment with Timer in thread pool thread. I haven't run into problems with this one yet, but I don't like it.
         * Need to `lock` which is quite expensive for our use case.
         *
         * I have the following idea. We should implement a timer via Unity's MonoBehaviour (based on Time.deltaTime) or coroutine. The executed code
         * will run on UI thread (in case of Unity 'Thread.CurrentThread.ManagedThreadId == 1').
         * So, we don't need to `lock` on anything.
         */
        private void TimerElapsed(object _, ElapsedEventArgs __)
        {
            // possible lock
            if (_eventContainerTime.Count == 0)
            {
                return;
            }

            // Debug.Log is "kinda" thread-safe.
            Debug.Log($"[Timer] Cleared! {Thread.CurrentThread.ManagedThreadId}.");

            // possible lock
            _eventContainerTime.Clear();
        }

        // Attempt.1
        public bool Debounced(LogType type, string condition)
        {
            var key = new Key(type, condition);
            if (!_eventContainer.Contains(key))
            {
                _eventContainer.Add(key);
                _barrierOffset = Now.Add(_barrierStep);

                Debug.Log(Colorize($"[True, {key.LogType}] Type count: {_eventContainer.Count}. Now: {Now:T}, BarrierOffset: {_barrierOffset:T}.", "green"));
                return true;
            }

            if (Now > _barrierOffset)
            {
                _eventContainer.Clear();
                _eventContainer.Add(key);
                _barrierOffset = Now.Add(_barrierStep);

                Debug.Log(Colorize($"[True Now>, {key.LogType}] Type count: {_eventContainer.Count}. Now: {Now:T}, BarrierOffset: {_barrierOffset:T}.", "yellow"));

                return true;
            }
            Debug.Log(Colorize($"[False, {key.LogType}] Type count: {_eventContainer.Count}. Now: {Now:T}, BarrierOffset: {_barrierOffset:T}.", "orange"));

            return false;
        }

        // Attempt.2
        public bool DebouncedTime(LogType type, string condition)
        {
            var key = new Key(type, condition);
            if (!_eventContainerTime.ContainsKey(key))
            {
                var barrierOffset = Now.Add(_barrierStep);
                _eventContainerTime.Add(key, barrierOffset);

                Debug.Log(Colorize($"[True, {key.LogType}] Type count: {_eventContainerTime.Count}. Now: {Now:T}, BarrierOffset: {_eventContainerTime[key]:T}.", "green"));
                return true;
            }

            if (Now > _eventContainerTime[key])
            {
                _eventContainerTime.Clear();

                var barrierOffset = Now.Add(_barrierStep);
                _eventContainerTime[key] = barrierOffset;

                Debug.Log(Colorize($"[True Now>, {key.LogType}] Type count: {_eventContainerTime.Count}. Now: {Now:T}, BarrierOffset: {_eventContainerTime[key]:T}.", "yellow"));
                return true;
            }

            Debug.Log(Colorize($"[False, {key.LogType}] Type count: {_eventContainerTime.Count}. Now: {Now:T}, BarrierOffset: {_eventContainerTime[key]:T}.", "orange"));

            return false;
        }

        private string Colorize(string message, string color)
            => $"<color={color}>{message}</color>";

        private readonly struct Key : IEquatable<Key>
        {
            public readonly LogType LogType;
            public readonly string Condition;

            public Key(LogType logType, string condition)
            {
                LogType = logType;
                Condition = condition;
            }

            public bool Equals(Key other)
                => LogType == other.LogType && Condition == other.Condition;

            public override bool Equals(object? obj)
                => obj is Key other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int) LogType * 397) ^ Condition.GetHashCode();
                }
            }
        }

        public void Dispose()
        {
            _timer.Elapsed -= TimerElapsed;
            _timer.Dispose();
        }
    }
}
