using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry.Unity
{
    internal interface IUnityLogMessageFilter
    {
        bool Debounced(LogType type, string condition);
    }

    internal sealed class TypeConditionFilter : IUnityLogMessageFilter
    {
        private readonly HashSet<Key> _eventContainer;
        private readonly TimeSpan _barrierStep;
        private DateTimeOffset _barrierOffset;

        /// <summary>
        /// Shortcut for UTC
        /// </summary>
        private static DateTimeOffset Now => DateTimeOffset.UtcNow;

        public TypeConditionFilter()
        {
            _eventContainer = new();
            _barrierStep = TimeSpan.FromSeconds(2);
            _barrierOffset = Now.Add(_barrierStep);
        }

        public bool Debounced(LogType type, string condition)
        {
            var key = new Key(type, condition);
            if (!_eventContainer.Contains(key))
            {
                _eventContainer.Add(key);
                _barrierOffset = Now.Add(_barrierStep);
                return true;
            }

            if (Now > _barrierOffset)
            {
                _eventContainer.Clear();
                _eventContainer.Add(key);
                _barrierOffset = Now.Add(_barrierStep);
                return true;
            }

            return false;
        }

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
    }
}
