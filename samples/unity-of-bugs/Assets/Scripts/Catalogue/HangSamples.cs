using System.Threading;
using UnityEngine;
using static Sentry.Unity.Samples.SampleHelpers;

namespace Sentry.Unity.Samples
{
    /// <summary>The hang samples: freezes in C# and in native code.</summary>
    public static class HangSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Hangs",
            new SampleEntry("hangs/main-thread-sleep", "Block the main thread",
                "Thread.Sleep for 10 s on the main thread. Arrives as an ANR or App Hang.",
                BlockMainThread),
            new SampleEntry("hangs/native-freeze", "Native freeze",
                "Sleeps 10 s on the main thread in native code: Kotlin on Android (ANR), Objective-C on iOS (App Hang).",
                FreezeNatively, SampleRequirements.AndroidOrIOS));

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
    }
}
