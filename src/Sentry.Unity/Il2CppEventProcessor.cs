using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Protocol;
using UnityEngine;

namespace Sentry.Unity
{
    internal class UnityIl2CppEventExceptionProcessor : ISentryEventExceptionProcessor
    {
        private readonly SentryUnityOptions _options;
        private readonly ISentryUnityInfo _sentryUnityInfo;
        private readonly Il2CppMethods _il2CppMethods;

        public UnityIl2CppEventExceptionProcessor(SentryUnityOptions options, ISentryUnityInfo sentryUnityInfo, Il2CppMethods il2CppMethods)
        {
            _options = options;
            _sentryUnityInfo = sentryUnityInfo;
            _il2CppMethods = il2CppMethods;
        }

        public void Process(Exception incomingException, SentryEvent sentryEvent)
        {
            _options.DiagnosticLogger?.LogDebug("Running Unity IL2CPP event exception processor on: Event {0}", sentryEvent.EventId);

            var sentryExceptions = sentryEvent.SentryExceptions;
            if (sentryExceptions == null)
            {
                return;
            }
            var exceptions = EnumerateChainedExceptions(incomingException);

            // Unity by definition only builds a single library/image,
            // which we add once to our list of debug images.
            var debugImages = (sentryEvent.DebugImages ??= new List<DebugImage>());
            // The il2cpp APIs give us image-relative instruction addresses, not
            // absolute ones. When processing events via symbolicator, we do want
            // to have absolute addresses. For this reason, we just add a sentinel
            // value to the `DebugImage` and `InstructionAddress`, which makes
            // addresses absolute and still associates the address with the one
            // and only `GameAssembly` image.
            var imageAddress = 0x1000;
            var didAddImage = false;

            foreach (var (sentryException, exception) in sentryExceptions.Zip(exceptions, (se, ex) => (se, ex)))
            {
                var sentryStacktrace = sentryException.Stacktrace;
                if (sentryStacktrace == null)
                {
                    // We will only augment an existing stack trace with native
                    // instructions, so with no stack trace, there is nothing to do
                    continue;
                }

                var nativeStackTrace = GetNativeStackTrace(exception);

                if (!didAddImage)
                {
                    var codeFile = nativeStackTrace.ImageName;
                    // NOTE: il2cpp in some circumstances does not return a correct `ImageName`.
                    // A null/missing `CodeFile` however would lead to a processing error in sentry.
                    // Since the code file is not strictly necessary for processing, we just fall back to
                    // a sentinel value here.
                    if (String.IsNullOrEmpty(codeFile))
                    {
                        codeFile = "GameAssembly.fallback";
                    }
                    debugImages.Add(new DebugImage
                    {
                        Type = _sentryUnityInfo.Platform,

                        CodeFile = codeFile,
                        DebugId = nativeStackTrace.ImageUuid,
                        ImageAddress = $"0x{imageAddress:X8}",
                    });

                    didAddImage = true;
                }

                var nativeLen = nativeStackTrace.Frames.Length;
                var len = Math.Min(sentryStacktrace.Frames.Count, nativeLen);
                for (var i = 0; i < len; i++)
                {
                    // The sentry stack trace is sorted parent (caller) to child (callee),
                    // whereas the native stack trace is sorted from callee to caller.
                    var frame = sentryStacktrace.Frames[i];
                    var nativeFrame = nativeStackTrace.Frames[nativeLen - 1 - i];

                    // The instructions in the stack trace generally have "return addresses"
                    // in them. But for symbolication, we want to symbolicate the address of
                    // the "call instruction", which in almost all cases happens to be
                    // the instruction right in front of the return address.
                    // A good heuristic to use in that case is to just subtract 1.
                    var instructionAddr = imageAddress + nativeFrame.ToInt64() - 1;
                    frame.InstructionAddress = $"0x{instructionAddr:X8}";
                }
            }
        }

        // This is the same logic as `MainExceptionProcessor` uses to create the `SentryEvent.SentryExceptions` list.
        // It yields chained Exceptions in innermost to outer Exception order.
        private IEnumerable<Exception> EnumerateChainedExceptions(Exception exception)
        {
            if (exception is AggregateException ae)
            {
                foreach (var inner in ae.InnerExceptions.SelectMany(EnumerateChainedExceptions))
                {
                    yield return inner;
                }
            }
            else if (exception.InnerException != null)
            {
                foreach (var inner in EnumerateChainedExceptions(exception.InnerException))
                {
                    yield return inner;
                }
            }
            yield return exception;
        }

        private NativeStackTrace GetNativeStackTrace(Exception e)
        {
            // Create a `GCHandle` for the exception, which we can then use to
            // essentially get a pointer to the underlying `Il2CppException` C++ object.
            var gch = GCHandle.Alloc(e);
            // The `il2cpp_native_stack_trace` allocates and writes the native
            // instruction pointers to the `addresses`/`numFrames` out-parameters.
            var addresses = IntPtr.Zero;
            try
            {
                var gchandle = GCHandle.ToIntPtr(gch).ToInt32();
                var addr = _il2CppMethods.Il2CppGcHandleGetTarget(gchandle);

                var numFrames = 0;
                string? imageUuid = null;
                string? imageName = null;
                _il2CppMethods.Il2CppNativeStackTrace(addr, out addresses, out numFrames, out imageUuid, out imageName);

                // Convert the C-Array to a managed "C#" Array, and free the underlying memory.
                var frames = new IntPtr[numFrames];
                Marshal.Copy(addresses, frames, 0, numFrames);

                return new NativeStackTrace
                {
                    Frames = frames,
                    ImageUuid = imageUuid,
                    ImageName = imageName,
                };
            }
            finally
            {
                // We are done with the `GCHandle`.
                gch.Free();

                if (addresses != IntPtr.Zero)
                {
                    _il2CppMethods.Il2CppFree(addresses);
                }
            }
        }
    }

    internal class NativeStackTrace
    {
        public IntPtr[] Frames { get; set; } = Array.Empty<IntPtr>();
        public string? ImageUuid { get; set; }
        public string? ImageName { get; set; }
    }
}
