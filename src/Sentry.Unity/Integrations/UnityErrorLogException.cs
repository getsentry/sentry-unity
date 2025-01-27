using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using Sentry.Extensibility;
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
    internal class UnityErrorLogException : Exception
    {
        internal static readonly string ExceptionType = "LogError";

        private readonly string _logString = string.Empty;
        private readonly string _logStackTrace = string.Empty;

        private readonly SentryOptions? _options;
        private readonly IDiagnosticLogger? _logger;

        public UnityErrorLogException(string logString, string logStackTrace, SentryOptions? options)
        {
            _logString = logString;
            _logStackTrace = logStackTrace;
            _options = options;
            _logger = _options?.DiagnosticLogger;
        }

        internal UnityErrorLogException() : base() { }

        private UnityErrorLogException(string message) : base(message) { }

        private UnityErrorLogException(string message, Exception innerException) : base(message, innerException) { }

        public SentryException ToSentryException()
        {
            _logger?.LogDebug("Creating SentryException out of synthetic ErrorLogException");

            var frames = ParseStackTrace(_logStackTrace);
            frames.Reverse();

            var stacktrace = new SentryStackTrace { Frames = frames };

            return new SentryException
            {
                Stacktrace = stacktrace,
                Type = ExceptionType,
                Value = _logString,
                Mechanism = new Mechanism
                {
                    Handled = true,
                    Type = "unity.log"
                }
            };
        }

        private const string AtFileMarker = " (at ";

        private List<SentryStackFrame> ParseStackTrace(string stackTrace)
        {
            // Example: Sentry.Unity.Integrations.UnityLogHandlerIntegration:LogFormat (UnityEngine.LogType,UnityEngine.Object,string,object[]) (at UnityLogHandlerIntegration.cs:89)
            // This follows the following format:
            // Module.Class.Method[.Invoke] (arguments) (at filepath:linenumber)
            // The ':linenumber' is optional and will be omitted in builds

            var frames = new List<SentryStackFrame>();
            var stackList = stackTrace.Split('\n');

            foreach (var line in stackList)
            {
                var item = line.TrimEnd('\r');
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }

                var frame = ParseStackFrame(item, _logger);
                if (_options is not null)
                {
                    frame.ConfigureAppFrame(_options);
                }
                frames.Add(frame);
            }

            return frames;
        }

        private static SentryStackFrame ParseStackFrame(string stackFrameLine, IDiagnosticLogger? logger = null)
        {
            var closingParenthesis = stackFrameLine.IndexOf(')');
            if (closingParenthesis == -1)
            {
                return CreateBasicStackFrame(stackFrameLine);
            }

            try
            {
                var functionName = stackFrameLine.Substring(0, closingParenthesis + 1);
                var remainingText = stackFrameLine.Substring(closingParenthesis + 1);

                if (!remainingText.StartsWith(AtFileMarker))
                {
                    // If it does not start with '(at' it's an unknown format. We're falling back to a basic stackframe
                    return CreateBasicStackFrame(stackFrameLine);
                }

                var (filename, lineNo) = ParseFileLocation(remainingText);
                var filenameWithoutZeroes = StripZeroes(filename);

                return new SentryStackFrame
                {
                    FileName = TryResolveFileNameForMono(filenameWithoutZeroes),
                    AbsolutePath = filenameWithoutZeroes,
                    Function = functionName,
                    LineNumber = lineNo == -1 ? null : lineNo
                };
            }
            catch (Exception e)
            {
                logger?.LogError(e, "Failed to parse the stack frame line {0}", stackFrameLine);

                // Suppress any errors while parsing and fall back to a basic stackframe
                return CreateBasicStackFrame(stackFrameLine);
            }
        }

        private static (string Filename, int LineNo) ParseFileLocation(string location)
        {
            // Remove " (at " prefix and trailing ")"
            var fileInfo = location.Substring(AtFileMarker.Length, location.Length - AtFileMarker.Length - 1);
            var lastColon = fileInfo.LastIndexOf(':');

            return lastColon == -1
                ? (fileInfo, -1)
                : (fileInfo.Substring(0, lastColon), int.Parse(fileInfo.Substring(lastColon + 1)));
        }

        private static SentryStackFrame CreateBasicStackFrame(string functionName) => new()
        {
            Function = functionName,
            FileName = null,
            AbsolutePath = null,
            LineNumber = null
        };

        // https://github.com/getsentry/sentry-unity/issues/103
        private static string StripZeroes(string filename)
            => filename.Replace("0", "").Equals("<>", StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : filename;

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
