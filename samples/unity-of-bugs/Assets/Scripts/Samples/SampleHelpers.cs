using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>What the samples of more than one category share.</summary>
    internal static class SampleHelpers
    {
        // Fields rather than constants, so the compiler doesn't flag the branches a platform never takes.
#if UNITY_WEBGL && !UNITY_EDITOR
        private static readonly bool HasThreads = false;
#else
        private static readonly bool HasThreads = true;
#endif

        /// <summary>Why the "Run on" choice can't run here, or null when it can.</summary>
        internal static string RunOnUnmetReason(RunOn runOn) =>
            HasThreads || runOn == RunOn.MainThread || runOn == RunOn.Coroutine ? null : "WebGL has no threads";

        // Runs the sample where the "Run on" selector says, and returns once it's done there. A choice that can't run
        // here runs on the main thread.
        internal static IEnumerator OnSelectedThread(Action sample)
        {
            var runOn = RunOnUnmetReason(SampleCatalogue.RunOn) == null ? SampleCatalogue.RunOn : RunOn.MainThread;
            Debug.Log($"Running on: {runOn}");
            switch (runOn)
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
                    throw new ArgumentOutOfRangeException(nameof(SampleCatalogue.RunOn), runOn, null);
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

        // 'NoInlining' ends up being inlined through IL2CPP anyway. :(
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static bool CheckSomeFakeWork() => DateTime.Now.Ticks > 0; // Always true but not optimizable

        internal static void CallKotlin(string method)
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
    }
}
