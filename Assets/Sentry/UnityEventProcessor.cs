using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity
{
    public class UnityEventProcessor : ISentryEventProcessor
    {
        public SentryEvent Process(SentryEvent @event)
        {
            // Add some Unity specific context:

            var version = "0.0.1-alpha";
            @event.Sdk.AddPackage("github:sentry.unity", version);
            @event.Sdk.Name = "sentry.unity";
            @event.Sdk.Version = version;

            @event.Contexts.OperatingSystem.Name = SystemInfo.operatingSystem;

            @event.Contexts.Device.Name = SystemInfo.deviceName;
#pragma warning disable RECS0018 // Value is exact when expressing no battery level
            if (SystemInfo.batteryLevel != -1.0)
#pragma warning restore RECS0018
            {
                @event.Contexts.Device.BatteryLevel = (short?)(SystemInfo.batteryLevel * 100);
            }

            @event.Release = Application.version;

            // This is the approximate amount of system memory in megabytes.
            // This function is not supported on Windows Store Apps and will always return 0.
            @event.Contexts.Device.MemorySize = SystemInfo.systemMemorySize;

            @event.Contexts.Device.Timezone = TimeZoneInfo.Local;

            @event.Contexts.App.StartTime = DateTimeOffset.UtcNow.AddSeconds(-Time.realtimeSinceStartup);

            @event.SetTag("unity:processorCount", SystemInfo.processorCount.ToString());
            @event.SetTag("unity:supportsVibration", SystemInfo.supportsVibration.ToString());
            @event.SetTag("unity:installMode", Application.installMode.ToString());

#if UNITY_EDITOR
            @event.Contexts.Device.Simulator = true;
#else
            evt.Contexts.Device.Simulator = false;
#endif

            return @event;
        }
    }

    public class UnityEventExceptionProcessor : ISentryEventExceptionProcessor
    {
        public void Process(Exception exception, SentryEvent sentryEvent)
        {
            if (exception is UnityLogException ule)
            {
                sentryEvent.SentryExceptions = new[] { GetException(ule.LogString, ule.LogStackTrace) };
            }
        }

        private static SentryException GetException(string condition, string stackTrace)
        {
            var frames = new List<SentryStackFrame>();
            var exc = condition.Split(new char[] { ':' }, 2);
            var excType = exc[0];
            var excValue = exc[1].Substring(1); // strip the space
            var stackList = stackTrace.Split('\n');

            // The format is as follows:
            // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
            // where :lineno is optional, will be ommitted in builds
            for (var i = 0; i < stackList.Length; i++)
            {
                string functionName;
                string filename;
                int lineNo;

                var item = stackList[i];
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

                frames.Add(new SentryStackFrame
                {
                    FileName = filename,
                    Function = functionName,
                    LineNumber = lineNo
                });
            }

            frames.Reverse();

            // TODO: Hack until we make it settable
            var stacktrace = new SentryStackTrace();
            var framesProp = typeof(SentryStackTrace).GetProperty("InternalFrames",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            framesProp.SetValue(stacktrace, frames);

            return new SentryException
            {
                Stacktrace = stacktrace,
                Type = excType,
                Value = excValue
            };
        }
    }
}
