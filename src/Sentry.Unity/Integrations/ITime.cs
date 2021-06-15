using System;
using UnityEngine;

namespace Sentry.Unity
{
    internal interface ITime
    {
        DateTime Now { get; }
    }

    internal sealed class TimeAdapter : ITime
    {
        public static readonly TimeAdapter Instance = new();

        private TimeAdapter()
        {
        }

        public DateTime Now => System.DateTime.Now;
    }
}
