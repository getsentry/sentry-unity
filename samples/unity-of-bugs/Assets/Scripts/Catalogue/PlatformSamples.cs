using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Sentry.Unity.Samples.SampleHelpers;

namespace Sentry.Unity.Samples
{
    /// <summary>The platform samples: Kotlin, Objective-C and running out of memory.</summary>
    public static class PlatformSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Platform",
            new SampleEntry("platform/kotlin-exception", "Kotlin exception",
                "Kotlin throws into C#, where it surfaces as an unhandled AndroidJavaException.",
                ThrowKotlin, SampleRequirements.Android),
            new SampleEntry("platform/kotlin-background-exception", "Kotlin exception on a background thread",
                "An uncaught exception on a Kotlin thread ends the app.",
                ThrowKotlinOnBackground, SampleRequirements.Android, crashes: true),
            new SampleEntry("platform/objective-c-exception", "Objective-C exception",
                "Raises an NSException in Objective-C, which ends the app.",
                ThrowObjectiveC, SampleRequirements.IOS, crashes: true),
            new SampleEntry("platform/out-of-memory", "Run out of memory",
                "Allocates every frame until iOS ends the app. Arrives as a watchdog termination on the next launch. Needs IosWatchdogTerminationIntegrationEnabled, which is off by default: this sample turns it on.",
                RunOutOfMemory, SampleRequirements.IOS, crashes: true));

        private static void ThrowKotlin() => CallKotlin("throw");

        private static void ThrowKotlinOnBackground() => CallKotlin("throwOnBackgroundThread");

        private static void ThrowObjectiveC()
        {
#if UNITY_IOS && !UNITY_EDITOR
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
    }
}
