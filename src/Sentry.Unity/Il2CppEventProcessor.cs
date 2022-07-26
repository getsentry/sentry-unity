using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Unity.NativeUtils;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

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
            var usedImages = new HashSet<DebugImage>();
            _logger = _options.DiagnosticLogger;

            // Unity usually produces stack traces with relative offsets in the GameAssembly library.
            // However, at least on Unity 2020 built Windows player, the offsets seem to be absolute.
            // Therefore, we try to determine which one it is, depending on whether they match the loaded libraries.
            // In case they don't we update the offsets to match the GameAssembly library.
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

                _options.DiagnosticLogger?.LogDebug("NativeStackTrace Image: '{0}' (UUID: {1})", nativeStackTrace.ImageName, nativeStackTrace.ImageUuid);

                // Unity by definition only builds a single library which we add once to our list of debug images.
                // We use this when we encounter stack frames with relative addresses.
                // We want to use an address that is definitely outside of any address range used by real libraries.
                // Canonical addresses on x64 leave a gap in the middle of the address space, which is unused.
                // This is a range of addresses that we should be able to safely use.
                // See https://en.wikipedia.org/wiki/X86-64#Virtual_address_space_details
                var mainLibOffset = (ulong)1 << 63;
                DebugImage? mainLibImage = null;

                // TODO do we really want to continue if these two don't match?
                //      Wouldn't it cause invalid frame info?
                var nativeLen = nativeStackTrace.Frames.Length;
                var len = Math.Min(sentryStacktrace.Frames.Count, nativeLen);
                for (var i = 0; i < len; i++)
                {
                    // The sentry stack trace is sorted parent->child (caller->callee),
                    // whereas the native stack trace is sorted from callee to caller.
                    var frame = sentryStacktrace.Frames[i];
                    var nativeFrame = nativeStackTrace.Frames[nativeLen - 1 - i];
                    var nativeImageUUID = NormalizeUUID(nativeStackTrace.ImageUuid);

                    // The instructions in the stack trace generally have "return addresses"
                    // in them. But for symbolication, we want to symbolicate the address of
                    // the "call instruction", which in almost all cases happens to be
                    // the instruction right in front of the return address.
                    // A good heuristic to use in that case is to just subtract 1.
                    var instructionAddress = (ulong)nativeFrame.ToInt64() - 1;
                    var image = FindDebugImageContainingAddress(instructionAddress);

                    if (image is not null)
                    {
                        _options.DiagnosticLogger?.Log(SentryLevel.Debug, "Stack frame '{0}' at {1:X8} belonging to debug image {2} based on instruction address.",
                            null, frame.Function, nativeFrame.ToInt64(), image.CodeFile);
                    }
                    else
                    {
                        // First, try to find the image by UUID.
                        var notes = "";
                        if (nativeImageUUID is not null)
                        {
                            image = DebugImagesSorted.Value.Find((info) => string.Equals(NormalizeUUID(info.Image.DebugId), nativeImageUUID))?.Image;
                            if (image is not null)
                            {
                                notes = " as determined by NativeStackTrace image UUID";
                            }
                        }

                        if (image is null)
                        {
                            mainLibImage ??= new DebugImage
                            {
                                Type = _sentryUnityInfo.Platform,
                                // NOTE: il2cpp in some circumstances does not return a correct `ImageName`.
                                // A null/missing `CodeFile` however would lead to a processing error in sentry.
                                // Since the code file is not strictly necessary for processing, we just fall back to
                                // a sentinel value here.
                                CodeFile = string.IsNullOrEmpty(nativeStackTrace.ImageName) ? "GameAssembly.fallback" : nativeStackTrace.ImageName,
                                DebugId = nativeStackTrace.ImageUuid,
                                ImageAddress = $"0x{mainLibOffset:X8}",
                            };
                            image = mainLibImage;
                        }

                        _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                            "Found stack frame '{0}' with a relative address {1:X8} inside debug image {2}{3}",
                            null, frame.Function, nativeFrame.ToInt64(), image.CodeFile, notes);

                        // Shift the instruction address to be absolute.
                        instructionAddress += Convert.ToUInt64(image.ImageAddress, 16);
                        frame.InstructionAddress = $"0x{instructionAddress:X8}";
                    }

                    _ = usedImages.Add(image);
                }
            }

            sentryEvent.DebugImages ??= new List<DebugImage>();
            sentryEvent.DebugImages.AddRange(usedImages);
        }

        // Normalizes Debug Image UUID so that we can compare the ones coming from
        // native (contains dashes, all lower-case) & what Unity gives us (no dashes, uppercase).
        // On Linux, the image also has shorter UUID coming from Unity, e.g. 3028cb80b0712541,
        // while native image UUID we get is 3028cb80-b071-2541-0000-000000000000.
        private static string? NormalizeUUID(string? value) =>
            value?.ToLowerInvariant().Replace("-", "").TrimEnd(new char[] { '0' });

        private class DebugImageInfo
        {
            public readonly DebugImage Image;
            public readonly ulong StartAddress;
            public readonly ulong EndAddress;

            public DebugImageInfo(DebugImage image)
            {
                Image = image;
                StartAddress = Convert.ToUInt64(image.ImageAddress, 16);
                EndAddress = StartAddress + (ulong)image.ImageSize!;
            }
        }

        private static IDiagnosticLogger? _logger;

        private static Lazy<List<DebugImageInfo>> DebugImagesSorted = new(() =>
        {
            var result = new List<DebugImageInfo>();
            var nativeDebugImages = C.DebugImages.Value;
            foreach (var image in nativeDebugImages)
            {
                if (image.ImageSize is null)
                {
                    _logger?.Log(SentryLevel.Debug,
                        "Skipping debug image '{0}' (CodeId {1} | DebugId: {2}) because its size is NULL",
                        null, image.CodeFile, image.CodeId, image.DebugId);
                    continue;
                }

                var info = new DebugImageInfo(image);
                int i = 0;
                for (; i < result.Count; i++)
                {
                    if (info.StartAddress < result[i].StartAddress)
                    {
                        // insert at index `i`, all the rest have a larger start address
                        break;
                    }
                }
                result.Insert(i, info);

                _logger?.Log(SentryLevel.Debug,
                    "Found debug image '{0}' (CodeId {1} | DebugId: {2}) with addresses between {3:X8} and {4:X8}",
                    null, image.CodeFile, image.CodeId, image.DebugId, info.StartAddress, info.EndAddress);
            }
            return result;
        });

        private static DebugImage? FindDebugImageContainingAddress(ulong instructionAddress)
        {
            var list = DebugImagesSorted.Value;
            foreach (var info in list)
            {
                if (info.StartAddress <= instructionAddress)
                {
                    if (info.EndAddress >= instructionAddress)
                    {
                        return info.Image;
                    }
                }
                else
                {
                    // no more images could match, because they're sorted by the StartAddress
                    break;
                }
            }
            return null;
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
