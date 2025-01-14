using System;
using System.Collections.Generic;
using System.IO;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity.Integrations
{
    /// <summary>
    /// An exception raised through the Application Logging Integration
    /// </summary>
    /// <remarks>
    /// <see cref="Application.logMessageReceived"/>
    /// </remarks>
    internal class UnityLogException : Exception
    {
        public string LogString { get; }
        public string LogStackTrace { get; }

        private SentryOptions? _options { get; }

        public UnityLogException(string logString, string logStackTrace, SentryOptions? options)
        {
            LogString = logString;
            LogStackTrace = logStackTrace;
            _options = options;
        }

        public UnityLogException(string logString, string logStackTrace)
        {
            LogString = logString;
            LogStackTrace = logStackTrace;
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

        public SentryException ToSentryException()
        {
            var frames = new List<SentryStackFrame>();
            var exc = LogString.Split(new char[] { ':' }, 2);
            var excType = exc[0];
            // TODO: condition may NOT contain ':' separator
            var excValue = exc.Length == 1 ? exc[0] : exc[1].Substring(1); // strip the space
            var stackList = LogStackTrace.Split('\n');

            // The format is as follows:
            // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
            // where :lineno is optional, will be ommitted in builds
            for (var i = 0; i < stackList.Length; i++)
            {
                string functionName;
                string filename;
                int lineNo;

                var item = stackList[i].TrimEnd('\r');
                if (item == string.Empty)
                {
                    continue;
                }

                var closingParen = item.IndexOf(')');

                if (closingParen == -1)
                {
                    functionName = item;
                    lineNo = -1;
                    filename = string.Empty;
                }
                else
                {
                    try
                    {
                        functionName = item.Substring(0, closingParen + 1);
                        if (item.Substring(closingParen + 1, 5) != " (at ")
                        {
                            // we did something wrong, failed the check
                            Debug.Log("failed parsing " + item);
                            functionName = item;
                            lineNo = -1;
                            filename = string.Empty;
                        }
                        else
                        {
                            var colon = item.LastIndexOf(':', item.Length - 1, item.Length - closingParen);
                            if (closingParen == item.Length - 1)
                            {
                                filename = string.Empty;
                                lineNo = -1;
                            }
                            else if (colon == -1)
                            {
                                filename = item.Substring(closingParen + 6, item.Length - closingParen - 7);
                                lineNo = -1;
                            }
                            else
                            {
                                filename = item.Substring(closingParen + 6, colon - closingParen - 6);
                                lineNo = Convert.ToInt32(item.Substring(colon + 1, item.Length - 2 - colon));
                            }
                        }
                    }
                    catch (Exception)
                    {
                        functionName = item;
                        lineNo = -1;
                        filename = string.Empty; // we have no clue
                    }
                }

                var filenameWithoutZeroes = StripZeroes(filename);
                var stackFrame = new SentryStackFrame
                {
                    FileName = TryResolveFileNameForMono(filenameWithoutZeroes),
                    AbsolutePath = filenameWithoutZeroes,
                    Function = functionName,
                    LineNumber = lineNo == -1 ? null : lineNo
                };
                if (_options is not null)
                {
                    stackFrame.ConfigureAppFrame(_options);
                }

                frames.Add(stackFrame);
            }

            frames.Reverse();

            var stacktrace = new SentryStackTrace();
            foreach (var frame in frames)
            {
                stacktrace.Frames.Add(frame);
            }

            return new SentryException
            {
                Stacktrace = stacktrace,
                Type = excType,
                Value = excValue,
                Mechanism = new Mechanism
                {
                    Handled = false,
                    Type = "unity.log"
                }
            };
        }

        // https://github.com/getsentry/sentry-unity/issues/103
        private static string StripZeroes(string filename)
            => filename.Equals("<00000000000000000000000000000000>", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : filename;

        // TODO: discuss
        private static string TryResolveFileNameForMono(string fileName)
        {
            try
            {
                // throws on Mono for <1231231231> paths
                return Path.GetFileName(fileName);
            }
            catch
            {
                // mono path
                return "Unknown";
            }
        }
    }
}
