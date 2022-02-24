using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Sentry.Internal;
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
        public LogType LogType { get; }
        public StackTraceLogType StackTraceLogType { get; }

        public UnityLogException(string logString, string logStackTrace, LogType logType, StackTraceLogType stackTraceLogType)
        {
            LogString = logString;
            LogStackTrace = logStackTrace;
            LogType = logType;
            StackTraceLogType = stackTraceLogType;
            Data[Mechanism.MechanismKey] = "unity.log";
        }

        internal UnityLogException() : base()
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
