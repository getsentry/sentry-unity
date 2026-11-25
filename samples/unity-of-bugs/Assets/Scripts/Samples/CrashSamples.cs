using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Diagnostics;
using static Sentry.Unity.Samples.SampleHelpers;

namespace Sentry.Unity.Samples
{
    /// <summary>The crash samples: the forced crash kinds and crashes in the native plugins.</summary>
    public static class CrashSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Crashes",
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
                () => Utils.ForceCrash(ForcedCrashCategory.MonoAbort), SampleRequirements.Mono, crashes: true),
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
                CatchViaCallback, SampleRequirements.IL2CPP));

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
    }
}
