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

        public SentryException ToSentryException()
        {
            if (LogType == LogType.Exception || StackTraceLogType != StackTraceLogType.Full)
            {
                return ConvertStackTrace();
            }

            return ConvertFullStackTrace();
        }

        private SentryException ConvertStackTrace()
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
                frames.Add(new SentryStackFrame
                {
                    FileName = TryResolveFileNameForMono(filenameWithoutZeroes),
                    AbsolutePath = filenameWithoutZeroes,
                    Function = functionName,
                    LineNumber = lineNo == -1 ? null : lineNo,
                    InApp = functionName != null
                        && !functionName.StartsWith("UnityEngine", StringComparison.Ordinal)
                        && !functionName.StartsWith("System", StringComparison.Ordinal)
                });
            }

            var stacktrace = new SentryStackTrace();
            foreach (var frame in Enumerable.Reverse(frames))
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

        private SentryException ConvertFullStackTrace()
        {
            if (LogStackTrace.StartsWith("0x")
                && LogStackTrace.Contains("(Unity) StackWalker::GetCurrentCallstack"))
            {
                return ConvertWindowsFullStackTraceImpl();
            }

            return ConvertFullStackTraceImpl();
        }

        private static readonly Regex WindowsStackFramePattern = new Regex(
            @"(?<addr>0x[0-9a-fA-F]+)\s*" + // 0x12345678
            @"\((?<module>.+?)\)\s*" + // (Unity), (Mono JIT Code), ...
            @"(\((?<wrapper>.+?)\)\s*)?" + // (wrapper managed-to-native),
            @"(\[(?<file>.+?):(?<line>\d+)\]\s*)?" + // [File.cs:123]
            @"(?<function>.+)", // StackWalker::GetCurrentCallstack, UnityEngine.Logger:Log (UnityEngine.LogType,UnityEngine.Object,string,object[]),
            RegexOptions.None,
            TimeSpan.FromSeconds(1));

        private SentryException ConvertWindowsFullStackTraceImpl()
        {
            var frames = new List<SentryStackFrame>();
            var lines = LogStackTrace.Split('\n');
            foreach (var traceLine in lines)
            {
                var item = traceLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                var match = WindowsStackFramePattern.Match(item);
                if (!match.Success)
                {
                    // we did something wrong, failed the check
                    Debug.Log("failed parsing " + item);
                    frames.Add(new SentryStackFrame
                    {
                        Function = item,
                    });
                }
                else
                {
                    var addr = match.Groups["addr"].Value;
                    var module = match.Groups["module"].Value;
                    var function = match.Groups["function"].Value;

                    string? wrapper = null;
                    if (match.Groups["wrapper"].Success)
                    {
                        wrapper = match.Groups["wrapper"].Value;
                    }

                    string? file = null;
                    if (match.Groups["file"].Success)
                    {
                        file = match.Groups["file"].Value;
                    }

                    int? line = null;
                    if (match.Groups["line"].Success)
                    {
                        line = Convert.ToInt32(match.Groups["line"].Value);
                    }

                    frames.Add(new SentryStackFrame
                    {
                        InstructionAddress = addr,
                        Module = module,
                        Function = function,
                        FileName = file,
                        LineNumber = line,
                        InApp = GuessInApp(module, function, wrapper, file),
                    });
                }
            }

            var stacktrace = new SentryStackTrace();
            foreach (var frame in Enumerable.Reverse(frames))
            {
                stacktrace.Frames.Add(frame);
            }

            return new SentryException
            {
                Stacktrace = stacktrace,
                Type = LogType.ToString(),
                Value = LogString,
                Mechanism = new Mechanism
                {
                    Handled = false,
                    Type = "unity.log"
                }
            };
        }

        private static readonly Regex FullStackFramePattern = new Regex(
            @"(?<index>#\d+)\s*" + // #0, #1, ...
            @"(\((?<module>.+?)\)\s*)?" + // (Unity), (Mono JIT Code), ...
            @"(\((?<wrapper>.+?)\)\s*)?" + // (wrapper managed-to-native),
            @"(\[(?<file>.+?):(?<line>\d+)\]\s*)?" + // [File.cs:123]
            @"(?<function>.+)", // StackWalker::GetCurrentCallstack, UnityEngine.Logger:Log (UnityEngine.LogType,UnityEngine.Object,string,object[]),
            RegexOptions.None,
            TimeSpan.FromSeconds(1)
        );

        private SentryException ConvertFullStackTraceImpl()
        {
            var frames = new List<SentryStackFrame>();
            var lines = LogStackTrace.Split('\n');
            foreach (var traceLine in lines)
            {
                var item = traceLine.TrimEnd('\r');
                if (string.IsNullOrWhiteSpace(item))
                {
                    continue;
                }

                var match = FullStackFramePattern.Match(item);
                if (!match.Success)
                {
                    // we did something wrong, failed the check
                    Debug.Log("failed parsing " + item);
                    frames.Add(new SentryStackFrame
                    {
                        Function = item,
                    });
                }
                else
                {
                    var module = match.Groups["module"].Value;
                    var function = match.Groups["function"].Value;

                    string? wrapper = null;
                    if (match.Groups["wrapper"].Success)
                    {
                        wrapper = match.Groups["wrapper"].Value;
                    }

                    string? file = null;
                    if (match.Groups["file"].Success)
                    {
                        file = match.Groups["file"].Value;
                    }

                    int? line = null;
                    if (match.Groups["line"].Success)
                    {
                        line = Convert.ToInt32(match.Groups["line"].Value);
                    }

                    frames.Add(new SentryStackFrame
                    {
                        Module = module,
                        Function = function,
                        FileName = file,
                        LineNumber = line,
                        InApp = GuessInApp(module, function, wrapper, file),
                    });
                }
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
                Type = LogType.ToString(),
                Value = LogString,
                Mechanism = new Mechanism
                {
                    Handled = false,
                    Type = "unity.log"
                }
            };
        }

        private static bool? GuessInApp(string module, string function, string? wrapper, string? file)
        {
            if (module == "Unity" || module == "KERNEL32" || module == "ntdll" || module == "USER32" || module == "ole32")
            {
                return false;
            }

            if (function.StartsWith("UnityEngine", StringComparison.Ordinal)
                || function.StartsWith("System", StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(wrapper))
            {
                return false;
            }

            if (file != null && file.EndsWith(".c"))
            {
                return false;
            }

            if (module == "Mono JIT Code" && file != null && file.EndsWith(".cs"))
            {
                return true;
            }

            return null;
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
