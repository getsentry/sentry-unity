using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Diagnostics;
using UnityEngine.SceneManagement;
#if SENTRY_HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif
#if SENTRY_HAS_DOTWEEN
using DG.Tweening;
#endif

namespace Sentry.Unity.Samples
{
    /// <summary>Where the error samples run their code.</summary>
    public enum RunOn
    {
        MainThread,
        Task,
        AwaitedTask,
        Coroutine,
        Thread,
    }

    /// <summary>
    /// Every sample the menu lists and a launch argument can run, by category.
    /// Each entry is a <c>new SampleEntry("category/name", ...</c> on a line of its own: tools read the IDs from them.
    /// </summary>
    public static class SampleCatalogue
    {
        public static readonly IReadOnlyList<SampleCategory> Categories = new[]
        {
            new SampleCategory("Errors",
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
                    Assert),
                new SampleEntry("errors/burst-job", "Burst job",
                    "Calls Debug.LogError from inside a Burst-compiled job.",
                    StartBuggyBurstJob)),

            new SampleCategory("Crashes",
                new SampleEntry("crashes/access-violation", "Access violation",
                    "Utils.ForceCrash(AccessViolation). The crash arrives on the next launch.",
                    () => Utils.ForceCrash(ForcedCrashCategory.AccessViolation), crashes: true),
                new SampleEntry("crashes/fatal-error", "Fatal error",
                    "Utils.ForceCrash(FatalError). The crash arrives on the next launch.",
                    () => Utils.ForceCrash(ForcedCrashCategory.FatalError), crashes: true),
                new SampleEntry("crashes/abort", "Abort",
                    "Utils.ForceCrash(Abort). The crash arrives on the next launch.",
                    () => Utils.ForceCrash(ForcedCrashCategory.Abort), crashes: true),
                new SampleEntry("crashes/pure-virtual-function", "Pure virtual function call",
                    "Utils.ForceCrash(PureVirtualFunction). The crash arrives on the next launch.",
                    () => Utils.ForceCrash(ForcedCrashCategory.PureVirtualFunction), crashes: true),
                new SampleEntry("crashes/mono-abort", "Mono abort",
                    "Utils.ForceCrash(MonoAbort). The crash arrives on the next launch.",
                    () => Utils.ForceCrash(ForcedCrashCategory.MonoAbort), crashes: true),
                new SampleEntry("crashes/native-c", "Crash in C",
                    "Dereferences a null pointer in a C plugin, called through C#.",
                    () => CallNested(NativeBridge.crash_in_c), SampleRequirements.IL2CPP, crashes: true),
                new SampleEntry("crashes/native-cpp", "Crash in C++",
                    "Dereferences a null pointer in a C++ plugin, called through C#.",
                    () => CallNested(NativeBridge.crash_in_cpp), SampleRequirements.IL2CPP, crashes: true),
                new SampleEntry("crashes/throw-cpp", "Uncaught C++ exception",
                    "Rethrows a std::runtime_error out of a C++ plugin, which ends the app.",
                    () => CallNested(NativeBridge.throw_cpp), SampleRequirements.IL2CPP, crashes: true),
                new SampleEntry("crashes/native-callback", "C# exception in a native callback",
                    "A C plugin calls back into C#, which throws, catches and captures. The app keeps running.",
                    CatchViaCallback, SampleRequirements.IL2CPP)),

            new SampleCategory("Platform",
                new SampleEntry("platform/kotlin-exception", "Kotlin exception",
                    "Kotlin throws into C#, where it surfaces as an unhandled AndroidJavaException.",
                    ThrowKotlin, SampleRequirements.Android),
                new SampleEntry("platform/kotlin-background-exception", "Kotlin exception on a background thread",
                    "An uncaught exception on a Kotlin thread ends the app.",
                    ThrowKotlinOnBackground, SampleRequirements.Android, crashes: true),
                new SampleEntry("platform/objective-c-exception", "Objective-C exception",
                    "Throws an NSException in Objective-C. Needs GCC_ENABLE_OBJC_EXCEPTIONS in Xcode.",
                    ThrowObjectiveC, SampleRequirements.IOS, crashes: true),
                new SampleEntry("platform/out-of-memory", "Run out of memory",
                    "Allocates every frame until iOS ends the app.",
                    RunOutOfMemory, SampleRequirements.IOS, crashes: true)),

            new SampleCategory("Hangs",
                new SampleEntry("hangs/main-thread-sleep", "Block the main thread",
                    "Thread.Sleep for 10 s on the main thread. Arrives as an ANR or App Hang.",
                    BlockMainThread),
                new SampleEntry("hangs/native-freeze", "Native freeze",
                    "Sleeps 10 s on the main thread in native code: Kotlin on Android (ANR), Objective-C on iOS (App Hang).",
                    FreezeNatively, SampleRequirements.AndroidOrIOS)),

            new SampleCategory("Context",
                new SampleEntry("context/set-user", "Set user",
                    "Sets ant@farm.bug as the user on every event that follows.",
                    SetUser),
                new SampleEntry("context/message-with-context", "Message with context",
                    "Captures a message with a custom PlayerCharacter context.",
                    CaptureMessageWithContext),
                new SampleEntry("context/set-tag", "Set tag",
                    "Sets the tag sample.tag on every event that follows.",
                    SetTag),
                new SampleEntry("context/add-breadcrumb", "Add breadcrumb",
                    "Adds a breadcrumb that the next event shows in its trail.",
                    AddBreadcrumb),
                new SampleEntry("context/attachment", "Message with an attachment",
                    "Adds a text file to the scope and captures a message that carries it.",
                    CaptureMessageWithAttachment),
                new SampleEntry("context/dropped-by-before-send", "Dropped by BeforeSend",
                    "Captures a message tagged SomeTag, which BeforeSend drops: nothing should arrive.",
                    CaptureDroppedMessage)),

            new SampleCategory("Tracing",
                new SampleEntry("tracing/transaction", "Transaction with spans",
                    "Starts a transaction and two child spans, then finishes them.",
                    RunTransaction)),

            new SampleCategory("Logs & Metrics",
                new SampleEntry("logs/structured-logs", "Structured logs",
                    "SentrySdk.Logger at info, warning and error.",
                    SendLogs),
                new SampleEntry("logs/sensitive-log", "Log dropped by BeforeSendLog",
                    "Logs a message starting with \"Sensitive:\", which BeforeSendLog drops: nothing should arrive.",
                    SendSensitiveLog),
                new SampleEntry("logs/metrics", "Metrics",
                    "SentrySdk.Metrics: a counter, a gauge and a distribution.",
                    EmitMetrics)),

            new SampleCategory("Feedback",
                new SampleEntry("feedback/widget", "Open the feedback widget",
                    "Opens the SentryUserFeedback form, the one behind the megaphone button.",
                    OpenFeedbackWidget, manualOnly: true),
                new SampleEntry("feedback/capture", "Capture feedback from code",
                    "SentrySdk.CaptureFeedback with a message, an email and a name.",
                    CaptureFeedback)),

            new SampleCategory("Scenes",
                new SampleEntry("scenes/load-single", "Load a scene",
                    "SceneManager.LoadScene. The load arrives as a transaction.",
                    () => LoadExtraScene(LoadSceneMode.Single)),
                new SampleEntry("scenes/load-additive", "Load a scene additively",
                    "Opens a scene on top of this one. Close unloads it.",
                    () => LoadExtraScene(LoadSceneMode.Additive)),
                new SampleEntry("scenes/load-async", "Load a scene asynchronously",
                    "SceneManager.LoadSceneAsync. The load arrives as a transaction.",
                    LoadExtraSceneAsync)),

            new SampleCategory("Third party",
                new SampleEntry("thirdparty/dotween-exception", "Exception in a DOTween callback",
                    "Throws from a tween's OnComplete.",
                    ThrowInTween, SampleRequirements.DOTween),
                new SampleEntry("thirdparty/unitask-exception", "Unhandled UniTask exception",
                    "A forgotten UniTask that throws after two delays.",
                    ThrowUniTaskException, SampleRequirements.UniTask),
                new SampleEntry("thirdparty/unitask-caught-exception", "Caught UniTask exception",
                    "Awaits a UniTask that throws, catches it and captures it.",
                    ThrowUniTaskExceptionWithCatch, SampleRequirements.UniTask)),
        };

        /// <summary>Where the error samples run their code. The menu's "Run on" selector sets it.</summary>
        public static RunOn RunOn { get; set; } = RunOn.MainThread;

        /// <summary>Set from the <c>run_id</c> launch argument. Logs and metrics carry it as the <c>sample.run_id</c> attribute.</summary>
        public static string RunId { get; set; }

        public static IEnumerable<SampleEntry> All
        {
            get
            {
                foreach (var category in Categories)
                {
                    foreach (var entry in category.Entries)
                    {
                        yield return entry;
                    }
                }
            }
        }

        public static SampleEntry Find(string id)
        {
            foreach (var entry in All)
            {
                if (entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }

        // ------------------------------------------------------------------------------------------------------------
        // Errors
        // ------------------------------------------------------------------------------------------------------------

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

        // Runs the sample where the "Run on" selector says, and returns once it's done there.
        private static IEnumerator OnSelectedThread(Action sample)
        {
            Debug.Log($"Running on: {RunOn}");
            switch (RunOn)
            {
                case RunOn.MainThread:
                    sample();
                    break;

                case RunOn.Task:
                    // Nothing observes the task: a throw arrives once the task is garbage collected.
                    var task = Task.Run(sample);
                    while (!task.IsCompleted)
                    {
                        yield return null;
                    }

                    if (task.IsFaulted)
                    {
                        SampleRunner.Report("Thrown in a task: the error arrives once the task is garbage collected");
                    }

                    break;

                case RunOn.AwaitedTask:
                    var done = false;
                    AwaitOnTask(sample, () => done = true);
                    while (!done)
                    {
                        yield return null;
                    }

                    // The rethrow from the async void method is posted to the main thread's next frame.
                    yield return null;
                    break;

                case RunOn.Coroutine:
                    SampleRunner.Instance.StartCoroutine(InCoroutine(sample));
                    // A frame for the coroutine to run and one for what it captured.
                    yield return null;
                    yield return null;
                    break;

                case RunOn.Thread:
                    var thread = new Thread(() => sample());
                    thread.Start();
                    while (thread.IsAlive)
                    {
                        yield return null;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(RunOn), RunOn, null);
            }
        }

        private static async void AwaitOnTask(Action sample, Action done)
        {
            try
            {
                await Task.Run(sample);
            }
            finally
            {
                done();
            }
        }

        private static IEnumerator InCoroutine(Action sample)
        {
            yield return null;
            sample();
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

        // 'NoInlining' ends up being inlined through IL2CPP anyway. :(
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool CheckSomeFakeWork() => DateTime.Now.Ticks > 0; // Always true but not optimizable

        private class CustomException : Exception
        {
            public CustomException(string message) : base(message)
            { }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Crashes
        // ------------------------------------------------------------------------------------------------------------

        // A few managed frames above the native call, for a stack trace that looks like a real one.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CallNested(Action native)
        {
            if (CheckSomeFakeWork())
            {
                CallNestedThere(native);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CallNestedThere(Action native)
        {
            if (CheckSomeFakeWork())
            {
                native();
            }
        }

        private static void CatchViaCallback() => NativeBridge.call_into_csharp(CSharpCallback);

        // Called from the C plugin.
        [AOT.MonoPInvokeCallback(typeof(NativeBridge.callback_t))]
        private static void CSharpCallback(int code)
        {
            try
            {
                throw new Exception($"C# exception triggered via native callback. Code = {code}");
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Platform
        // ------------------------------------------------------------------------------------------------------------

        private static void ThrowKotlin() => CallKotlin("throw");

        private static void ThrowKotlinOnBackground() => CallKotlin("throwOnBackgroundThread");

        private static void CallKotlin(string method)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using (var plugin = new AndroidJavaClass("unity.of.bugs.KotlinPlugin"))
            {
                plugin.CallStatic(method);
            }
#else
            Debug.LogWarning($"KotlinPlugin.{method} needs an Android player.");
#endif
        }

        private static void ThrowObjectiveC()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Debug.Log("The iOS SDK captures Objective-C exceptions. Consider enabling 'GCC_ENABLE_OBJC_EXCEPTIONS' in the Xcode build settings.");
            NativeBridge.throwObjectiveC();
#else
            Debug.LogWarning("Throwing an Objective-C exception needs an iOS player.");
#endif
        }

        private static IEnumerator RunOutOfMemory()
        {
            Debug.Log("Attempting to run out of memory");
            var memoryEaters = new List<object[]>();
            while (true)
            {
                yield return null;
                memoryEaters.Add(new object[1000000]);
            }
        }

        // ------------------------------------------------------------------------------------------------------------
        // Hangs
        // ------------------------------------------------------------------------------------------------------------

        private static void BlockMainThread()
        {
            Debug.Log("Running Thread.Sleep() on the main thread to trigger an ANR event.");
            Thread.Sleep(10 * 1000); // ANR detection currently defaults to 5 seconds
            Debug.Log("Thread.Sleep() finished.");
        }

        private static void FreezeNatively()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("Stalling the main thread via Kotlin to trigger a native ANR event.");
            CallKotlin("applicationNotResponding");
#elif UNITY_IOS && !UNITY_EDITOR
            Debug.Log("Stalling the main thread via Objective-C to trigger a native App Hang event.");
            NativeBridge.applicationNotResponding();
#else
            Debug.LogWarning("The native freeze needs an Android or iOS player.");
#endif
        }

        // ------------------------------------------------------------------------------------------------------------
        // Context
        // ------------------------------------------------------------------------------------------------------------

        private static void SetUser()
        {
            // Setting the user on the scope makes sure the user is set on the context of all future events
            SentrySdk.ConfigureScope(s =>
            {
                s.User = new SentryUser
                {
                    Email = "ant@farm.bug",
                    Username = "ant",
                    Id = "ant-id"
                };
            });
            Debug.Log("User set: ant");
            SampleRunner.Report("User set: ant");
        }

        private class PlayerCharacter
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string AttackType { get; set; }
        }

        private static void CaptureMessageWithContext()
        {
            // The context is freely customizable and allows you to add data specific to your game.
            // The SDK's capture methods take an optional scope that only applies to that one event.
            // An event captured with a scope callback isn't the hub's LastEventId: its ID is the return value.
            var eventId = SentrySdk.CaptureMessage("Capturing with player character context.", scope =>
            {
                scope.Contexts["character"] = new PlayerCharacter
                {
                    Name = "Mighty Fighter",
                    Age = 19,
                    AttackType = "melee"
                };
            });
            SampleRunner.Report($"Last event: {eventId}");
        }

        private static void SetTag()
        {
            SentrySdk.SetTag("sample.tag", "set-from-the-menu");
            SampleRunner.Report("Tag set: sample.tag");
        }

        private static void AddBreadcrumb()
        {
            SentrySdk.AddBreadcrumb("The player picked up a 🐞", "sample", level: BreadcrumbLevel.Info);
            SampleRunner.Report("Breadcrumb added");
        }

        private static void CaptureMessageWithAttachment()
        {
            var text = $"Captured at {DateTime.UtcNow:O} by the Unity of Bugs sample.";
            var eventId = SentrySdk.CaptureMessage("Message with an attachment", scope =>
                scope.AddAttachment(Encoding.UTF8.GetBytes(text), "sample-attachment.txt", AttachmentType.Default, "text/plain"));
            SampleRunner.Report($"Last event: {eventId}");
        }

        private static void CaptureDroppedMessage()
        {
            // SentryOptionConfiguration's BeforeSend drops events with the tag SomeTag
            SentrySdk.CaptureMessage("This message is dropped by BeforeSend", scope => scope.SetTag("SomeTag", "dropped"));
            SampleRunner.Report("Dropped by BeforeSend: nothing should arrive");
        }

        // ------------------------------------------------------------------------------------------------------------
        // Tracing
        // ------------------------------------------------------------------------------------------------------------

        private static IEnumerator RunTransaction()
        {
            var transaction = SentrySdk.StartTransaction("Unity of Bugs sample", "sample.transaction");

            var load = transaction.StartChild("sample.load", "Load the level");
            yield return new WaitForSeconds(0.2f);
            load.Finish();

            var spawn = transaction.StartChild("sample.spawn", "Spawn the bugs");
            yield return new WaitForSeconds(0.3f);
            spawn.Finish();

            transaction.Finish();
            SampleRunner.Report($"Transaction sent: trace {transaction.TraceId}");
        }

        // ------------------------------------------------------------------------------------------------------------
        // Logs & Metrics
        // ------------------------------------------------------------------------------------------------------------

        private static void SendLogs()
        {
            SentrySdk.Logger.LogInfo(AddRunId, "The 🐛s awaken!");
            SentrySdk.Logger.LogWarning(AddRunId, "Here come the bugs 🐞🦋🐛🐜🕷!");
            SentrySdk.Logger.LogError(AddRunId, "A bug got away 🦋");
            SampleRunner.Report("Logs sent: info, warning and error");
        }

        private static void SendSensitiveLog()
        {
            // SentryOptionConfiguration's BeforeSendLog drops logs that start with "Sensitive:"
            SentrySdk.Logger.LogInfo(AddRunId, "Sensitive: the player's home address");
            SampleRunner.Report("Dropped by BeforeSendLog: nothing should arrive");
        }

        private static void EmitMetrics()
        {
            SentrySdk.Metrics.EmitCounter("sample.bugs_squashed", 1, RunIdAttributes());
            SentrySdk.Metrics.EmitGauge("sample.bugs_alive", 42, MeasurementUnit.None, RunIdAttributes());
            SentrySdk.Metrics.EmitDistribution("sample.squash_time", 0.25, MeasurementUnit.Duration.Second, RunIdAttributes());
            SampleRunner.Report("Metrics sent: counter, gauge and distribution");
        }

        private static void AddRunId(SentryLog log)
        {
            if (!string.IsNullOrEmpty(RunId))
            {
                log.SetAttribute("sample.run_id", RunId);
            }
        }

        private static Dictionary<string, object> RunIdAttributes()
        {
            var attributes = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(RunId))
            {
                attributes["sample.run_id"] = RunId;
            }

            return attributes;
        }

        // ------------------------------------------------------------------------------------------------------------
        // Feedback
        // ------------------------------------------------------------------------------------------------------------

        private static void OpenFeedbackWidget()
        {
            foreach (var feedback in Resources.FindObjectsOfTypeAll<SentryUserFeedback>())
            {
                if (feedback.gameObject.scene.IsValid())
                {
                    feedback.ShowFeedbackForm();
                    SampleRunner.Report("Feedback form open");
                    return;
                }
            }

            SampleRunner.Report("This scene has no SentryUserFeedback");
        }

        private static void CaptureFeedback()
        {
            var id = SentrySdk.CaptureFeedback("The bugs are too friendly 🐞", "ant@farm.bug", "ant");
            SampleRunner.Report($"Feedback sent: {id}");
        }

        // ------------------------------------------------------------------------------------------------------------
        // Scenes
        // ------------------------------------------------------------------------------------------------------------

        // Each returns once the scene is loaded and the frame after it, when the SDK finishes the scene's transaction.
        private static IEnumerator LoadExtraScene(LoadSceneMode mode)
        {
            ExtraScene.LoadedAdditively = mode == LoadSceneMode.Additive;
            SceneManager.LoadScene(ExtraScene.Name, mode);
            while (!SceneManager.GetSceneByName(ExtraScene.Name).isLoaded)
            {
                yield return null;
            }

            yield return null;
            SampleRunner.Report($"Loaded {ExtraScene.Name} ({mode})");
        }

        private static IEnumerator LoadExtraSceneAsync()
        {
            ExtraScene.LoadedAdditively = false;
            yield return SceneManager.LoadSceneAsync(ExtraScene.Name);
            yield return null;
            SampleRunner.Report($"Loaded {ExtraScene.Name} asynchronously");
        }

        // ------------------------------------------------------------------------------------------------------------
        // Third party
        // ------------------------------------------------------------------------------------------------------------

        private static void ThrowInTween()
        {
#if SENTRY_HAS_DOTWEEN
            var target = new GameObject("Tweened").transform;
            target.DOPunchScale(new Vector3(1.1f, 1.1f), 0.5f)
                .OnComplete(() =>
                {
                    UnityEngine.Object.Destroy(target.gameObject);
                    throw new NullReferenceException("There is nothing on complete.");
                });
#endif
        }

        private static void ThrowUniTaskException()
        {
#if SENTRY_HAS_UNITASK
            Debug.Log("Starting UniTask that will throw an exception! 🚀");
            DoAsyncWorkAndThrow().Forget();
#endif
        }

        private static void ThrowUniTaskExceptionWithCatch()
        {
#if SENTRY_HAS_UNITASK
            Debug.Log("Starting UniTask with proper error handling! 🛡️");
            DoAsyncWorkAndThrowWithCatch().Forget();
#endif
        }

#if SENTRY_HAS_UNITASK
        private static async UniTask DoAsyncWorkAndThrow()
        {
            Debug.Log("UniTask started - doing some async work... 🔄");

            await UniTask.Delay(200);
            Debug.Log("UniTask work phase 1 complete ✅");

            await UniTask.Delay(200);
            Debug.Log("UniTask work phase 2 complete ✅");

            throw new InvalidOperationException("UniTask async operation failed! 💥🐛");
        }

        private static async UniTask DoAsyncWorkAndThrowWithCatch()
        {
            try
            {
                await DoAsyncWorkAndThrow();
            }
            catch (Exception e)
            {
                Debug.Log("Caught UniTask exception - sending to Sentry 📡");
                SentrySdk.CaptureException(e);
            }
        }
#endif
    }

    public sealed class SampleCategory
    {
        public string Name { get; }
        public IReadOnlyList<SampleEntry> Entries { get; }

        public SampleCategory(string name, params SampleEntry[] entries)
        {
            Name = name;
            Entries = entries;
            foreach (var entry in entries)
            {
                entry.Category = name;
            }
        }
    }
}
