using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using static Sentry.Unity.Samples.SampleHelpers;

namespace Sentry.Unity.Samples
{
    /// <summary>The error samples: exceptions, messages and logs. The menu's "Run on" selector says where most of them run.</summary>
    public static class ErrorSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Errors",
            new SampleEntry("errors/unhandled-exception", "Unhandled exception",
                "Throws a custom exception through a few calls. Arrives as an unhandled error.",
                () => OnSelectedThread(ThrowUnhandledException)),
            new SampleEntry("errors/caught-exception", "Caught exception",
                "Throws, catches it and captures it with SentrySdk.CaptureException.",
                () => OnSelectedThread(ThrowExceptionButCatch)),
            new SampleEntry("errors/capture-message", "Capture message",
                "SentrySdk.CaptureMessage. Messages carry a stack trace with AttachStacktrace.",
                () => OnSelectedThread(CaptureMessage)),
            new SampleEntry("errors/log-error", "Debug.LogError",
                "Arrives as a message event, with Unity's stack trace.",
                () => OnSelectedThread(LogError)),
            new SampleEntry("errors/log-exception", "Debug.LogException",
                "Logs a caught exception. Arrives as an unhandled error.",
                () => OnSelectedThread(LogException)),
            new SampleEntry("errors/assert", "Failed assertion",
                "UnityEngine.Assertions.Assert.IsTrue(false). Arrives as an error.",
                Assert, SampleRequirements.Assertions),
            new SampleEntry("errors/burst-job", "Burst job",
                "Calls Debug.LogError from inside a Burst-compiled job.",
                StartBuggyBurstJob));

        private static void ThrowUnhandledException()
        {
            Debug.Log("Throwing an unhandled 🕷 exception!");
            DoSomeWorkHere();
        }

        private static void ThrowExceptionButCatch()
        {
            Debug.Log("Throwing an exception but catching it! 🐜");

            try
            {
                DoSomeWorkHere();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        }

        private static void CaptureMessage()
        {
            if (CheckSomeFakeWork())
            {
                // Messages do not have a stack trace by default. AttachStacktrace adds one, but IL2CPP cannot add
                // source lines to it. See known limitations:
                // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
                SentrySdk.CaptureMessage("🕷️🕷️🕷️ Spider message 🕷️🕷️🕷️🕷️");
            }
        }

        private static void LogError()
        {
            if (CheckSomeFakeWork())
            {
                // Error logs are captured as messages and do not have a stack trace by default. AttachStacktrace parses
                // Unity's stack trace. Unity 6 can include source lines when configured in Player Settings. See:
                // https://docs.sentry.io/platforms/unity/troubleshooting/known-limitations/#line-numbers-missing-in-events-captured-through-debuglogerror-or-sentrysdkcapturemessage
                Debug.LogError("This is a 'Debug.LogError()' message.");
            }
        }

        private static void LogException()
        {
            if (CheckSomeFakeWork())
            {
                try
                {
                    throw new NullReferenceException("Some bugs are harder to catch than others. 🦋");
                }
                catch (Exception e)
                {
                    // Logged exceptions are captured automatically, marked as unhandled like an unhandled exception.
                    // Capturing it with `SentrySdk.CaptureException(e);` is preferred.
                    Debug.LogException(e);
                }
            }
        }

        private static void Assert() => UnityEngine.Assertions.Assert.IsTrue(false);

        [BurstCompile]
        private struct BuggyBurstJob : IJob
        {
            public void Execute()
            {
                Debug.LogError("Bursting with bugs! 💥");
            }
        }

        private static void StartBuggyBurstJob()
        {
            Debug.Log("Starting Burst job filled with bugs! 💥");
            var job = new BuggyBurstJob();
            var handle = job.Schedule();
            handle.Complete();
        }

        // We're checking some fake work here to prevent too aggressive optimization. That way, we can show off some
        // proper stack traces that are closer to real-world bugs and events.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoSomeWorkHere()
        {
            if (CheckSomeFakeWork())
            {
                DoSomeWorkThere();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void DoSomeWorkThere()
        {
            if (CheckSomeFakeWork())
            {
                throw new CustomException("Exception from an exceptional lady beetle 🐞!");
            }
        }

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message)
            { }
        }
    }
}
