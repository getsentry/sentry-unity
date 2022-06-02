using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry.Unity
{
    // TODO: Make sure this whole functionality/class is only compiled when:
    // * Compiling for the il2cpp backend.
    // * Using Unity 2020 or later, as we use internal `libil2cpp` APIs that are
    //   only available there.

    internal class UnityIl2CppEventExceptionProcessor : ISentryEventExceptionProcessor
    {
        private readonly Il2CppMethods _il2CppMethods;

        public UnityIl2CppEventExceptionProcessor(Il2CppMethods il2CppMethods) => _il2CppMethods = il2CppMethods;

        public void Process(Exception incomingException, SentryEvent sentryEvent)
        {
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
            // absolute ones, and we also do not get the image addr.
            // For this reason we will use the "rel:N" AddressMode, giving the
            // index of the image in the list of all debug images.
            string? addrMode = null;

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

                if (addrMode == null)
                {
                    var imageIdx = debugImages.Count;
                    debugImages.Add(new DebugImage
                    {
                        // NOTE: This obviously is not wasm, but that type is used for
                        // images that do not have a `image_addr` but are rather used with "rel:N" AddressMode.
                        Type = "wasm",
                        CodeFile = nativeStackTrace.ImageName,
                        DebugId = nativeStackTrace.ImageUuid,
                    });
                    addrMode = "rel:" + imageIdx;
                }

                var nativeLen = nativeStackTrace.Frames.Length;
                var len = Math.Min(sentryStacktrace.Frames.Count, nativeLen);
                for (int i = 0; i < len; i++)
                {
                    // The sentry stack trace is sorted parent (caller) to child (callee),
                    // whereas the native stack trace is sorted from callee to caller.
                    var frame = sentryStacktrace.Frames[i];
                    var nativeFrame = nativeStackTrace.Frames[nativeLen - 1 - i];
                    frame.InstructionAddress = $"0x{nativeFrame:X8}";
                    frame.AddressMode = addrMode;
                }
            }
        }

        // This is the same logic as `MainExceptionProcessor` uses to create the `SentryEvent.SentryExceptions` list.
        // It yields chained Exceptions in innermost to outer Exception order.
        internal IEnumerable<Exception> EnumerateChainedExceptions(Exception exception)
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
                string? imageUUID = null;
                string? imageName = null;
                _il2CppMethods.Il2CppNativeStackTrace(addr, out addresses, out numFrames, out imageUUID, out imageName);

                // Convert the C-Array to a managed "C#" Array, and free the underlying memory.
                var frames = new IntPtr[numFrames];
                Marshal.Copy(addresses, frames, 0, numFrames);

                return new NativeStackTrace
                {
                    Frames = frames,
                    ImageUuid = imageUUID,
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
