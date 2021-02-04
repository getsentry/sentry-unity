using System;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity
{
    /// <summary>
    /// An exception raised through the Unity logging callback
    /// </summary>
    /// <remarks>
    /// <see cref="Application.logMessageReceived"/>
    /// </remarks>
    internal class UnityLogException : Exception
    {
        public string LogString { get; }
        public string LogStackTrace { get; }

        public UnityLogException(string logString, string logStackTrace)
        {
            LogString = logString;
            LogStackTrace = logStackTrace;
            Data[Mechanism.MechanismKey] = "unity.log";
        }

        private UnityLogException() : base()
        {
            LogString = "";
            LogStackTrace = "";
        }

        private UnityLogException(string message) : base(message)
        {
            LogString = "";
            LogStackTrace = "";
        }

        private UnityLogException(string message, Exception innerException) : base(message, innerException)
        {
            LogString = "";
            LogStackTrace = "";
        }
    }
}
