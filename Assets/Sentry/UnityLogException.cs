using System;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// An exception raised through the Unity logging callback
    /// </summary>
    /// <remarks>
    /// <see cref="Application.logMessageReceived"/>
    /// </remarks>
    public class UnityLogException : Exception
    {
        public string LogString { get; set; }
        public string LogStackTrace { get; set; }

        public UnityLogException(string logString, string logStackTrace)
        {
            LogString = logString;
            LogStackTrace = logStackTrace;
        }
    }
}
