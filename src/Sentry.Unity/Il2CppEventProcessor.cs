using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using Sentry.Unity.Integrations;
using Sentry.Unity.NativeUtils;
using UnityEngine;
using Application = UnityEngine.Application;

namespace Sentry.Unity
{
    internal class UnityIl2CppEventProcessor : ISentryEventProcessor, ISentryEventExceptionProcessor
    {
        private readonly SentryUnityOptions _options;
        private static ISentryUnityInfo UnityInfo = null!; // private static will be initialized in the constructor
        private readonly Il2CppMethods _il2CppMethods;

        public UnityIl2CppEventProcessor(SentryUnityOptions options, ISentryUnityInfo unityInfo)
        {
            _options = options;
            UnityInfo = unityInfo;
            _il2CppMethods = unityInfo.Il2CppMethods ?? throw new ArgumentNullException(nameof(unityInfo.Il2CppMethods), "Unity IL2CPP methods are not available.");

            _options.SdkIntegrationNames.Add("IL2CPPLineNumbers");
        }

        public SentryEvent Process(SentryEvent @event)
        {
            _options.DiagnosticLogger?.LogDebug("Running Unity IL2CPP event processor on: Event {0}", @event.EventId);

            var sentryExceptions = @event.SentryExceptions;
            if (sentryExceptions?.Any() == true)
            {
                return @event;
            }

            var usedImages = new HashSet<DebugImage>();
            Logger = _options.DiagnosticLogger;

            if (@event.SentryThreads != null)
            {
                foreach (var thread in @event.SentryThreads)
                {
                    var sentryStacktrace = thread.Stacktrace;
                    if (sentryStacktrace == null)
                    {
                        // We will only augment an existing stack trace with native
                        // instructions, so with no stack trace, there is nothing to do
                        return @event;
                    }

                    sentryStacktrace.AddressAdjustment =
                        Application.platform == RuntimePlatform.Android
                            ? InstructionAddressAdjustment.None
                            : InstructionAddressAdjustment.All;

                    var nativeStackTrace = GetNativeStackTrace();

                    _options.DiagnosticLogger?.LogDebug("NativeStackTrace Image: '{0}' (UUID: {1})", nativeStackTrace.ImageName, nativeStackTrace.ImageUuid);

                    ProcessNativeCallStack(sentryStacktrace, nativeStackTrace, usedImages);
                }
            }

            @event.DebugImages ??= new List<DebugImage>();
            @event.DebugImages.AddRange(usedImages);

            return @event;
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
            Logger = _options.DiagnosticLogger;

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

                sentryStacktrace.AddressAdjustment =
                    Application.platform == RuntimePlatform.Android
                        ? InstructionAddressAdjustment.None
                        : InstructionAddressAdjustment.All;

                var nativeStackTrace = GetNativeStackTrace(exception);

                _options.DiagnosticLogger?.LogDebug("NativeStackTrace Image: '{0}' (UUID: {1})", nativeStackTrace.ImageName, nativeStackTrace.ImageUuid);

                ProcessNativeCallStack(sentryStacktrace, nativeStackTrace, usedImages);
            }

            sentryEvent.DebugImages ??= new List<DebugImage>();
            sentryEvent.DebugImages.AddRange(usedImages);
        }

        private void ProcessNativeCallStack(SentryStackTrace sentryStacktrace, NativeStackTrace nativeStackTrace, HashSet<DebugImage> usedImages)
        {
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
            var eventLen = sentryStacktrace.Frames.Count;
            if (nativeLen != eventLen)
            {
                _options.DiagnosticLogger?.LogWarning(
                    "Native and sentry stack trace lengths don't match '({0} != {1})' - this may cause invalid stack traces.",
                    nativeLen, eventLen);
            }

            var len = Math.Min(eventLen, nativeLen);
            for (var i = 0; i < len; i++)
            {
                // The sentry stack trace is sorted parent->child (caller->callee),
                // whereas the native stack trace is sorted from callee to caller.
                var frame = sentryStacktrace.Frames[i];
                var nativeFrame = nativeStackTrace.Frames[nativeLen - 1 - i];
                var mainImageUUID = NormalizeUuid(nativeStackTrace.ImageUuid);

                // TODO should we do this for all addresses or only relative ones?
                //      If the former, we should also update `frame.InstructionAddress` down below.
                var instructionAddress = (ulong)nativeFrame.ToInt64();

                // We cannot determine whether this frame is a main library frame just from the address
                // because even relative address on the frame may correspond to an absolute address of a loaded library.
                // Therefore, if the frame package matches known prefixes, we assume it's a GameAssembly frame.
                var isMainLibFrame = frame.Package is not null && (
                    frame.Package.StartsWith("UnityEngine.", StringComparison.InvariantCultureIgnoreCase) ||
                    frame.Package.StartsWith("Assembly-CSharp", StringComparison.InvariantCultureIgnoreCase)
                );

                string? notes = null;
                DebugImage? image = null;
                bool? isRelativeAddress = null;
                if (!isMainLibFrame)
                {
                    image = FindDebugImageContainingAddress(instructionAddress);
                    if (image is null)
                    {
                        isRelativeAddress = true;
                        notes = "because it looks like a relative address.";
                        // falls through to the next `if (image is null)`
                    }
                    else
                    {
                        isRelativeAddress = false;
                        notes = "because it looks like an absolute address inside the range of this debug image.";
                    }
                }

                if (image is null)
                {
                    if (mainImageUUID is null)
                    {
                        _options.DiagnosticLogger?.LogWarning("Couldn't process stack trace - main image UUID reported as NULL by Unity");
                        continue;
                    }

                    // First, try to find the image among the loaded ones, otherwise create a dummy one.
                    mainLibImage ??= DebugImagesSorted.Value.Find((info) => string.Equals(NormalizeUuid(info.Image.DebugId), mainImageUUID))?.Image;
                    mainLibImage ??= new DebugImage
                    {
                        Type = UnityInfo.GetDebugImageType(Application.platform),
                        // NOTE: il2cpp in some circumstances does not return a correct `ImageName`.
                        // A null/missing `CodeFile` however would lead to a processing error in sentry.
                        // Since the code file is not strictly necessary for processing, we just fall back to
                        // a sentinel value here.
                        CodeFile = string.IsNullOrEmpty(nativeStackTrace.ImageName) ? "GameAssembly.fallback" : nativeStackTrace.ImageName,
                        DebugId = mainImageUUID,
                        ImageAddress = $"0x{mainLibOffset:X8}",
                    };

                    image = mainLibImage;
                    if (isMainLibFrame)
                    {
                        notes ??= $"based on frame package name ({frame.Package}).";
                    }
                }

                var imageAddress = Convert.ToUInt64(image.ImageAddress, 16);
                isRelativeAddress ??= instructionAddress < imageAddress;

                if (isRelativeAddress is true)
                {
                    // Shift the instruction address to be absolute.
                    instructionAddress += imageAddress;
                    frame.InstructionAddress = $"0x{instructionAddress:X8}";
                }

                // sanity check that the instruction fits inside the range
                var logLevel = SentryLevel.Debug;
                if (image.ImageSize is not null)
                {
                    if (instructionAddress < imageAddress || instructionAddress > imageAddress + (ulong)image.ImageSize)
                    {
                        logLevel = SentryLevel.Warning;
                        notes ??= ".";
                        notes += " However, the instruction address falls out of the range of the debug image.";
                    }
                }

                _options.DiagnosticLogger?.Log(logLevel, "Stack frame '{0}' at {1:X8} (originally {2:X8}) belongs to {3} {4}",
                    null, frame.Function, instructionAddress, nativeFrame.ToInt64(), image.CodeFile, notes ?? "");

                _ = usedImages.Add(image);
            }
        }

        // Normalizes Debug Image UUID so that we can compare the ones coming from
        // native (contains dashes, all lower-case) & what Unity gives us (no dashes, uppercase).
        // On Linux, the image also has shorter UUID coming from Unity, e.g. 3028cb80b0712541,
        // while native image UUID we get is 3028cb80-b071-2541-0000-000000000000.
        internal static string? NormalizeUuid(string? value)
        {
            if (value is null)
            {
                return null;
            }

            value = value.ToLowerInvariant();
            value = value.Replace("-0000-000000000000", "");
            return value.Replace("-", "");
        }

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

            public bool ContainsAddress(ulong address) => StartAddress <= address && address <= EndAddress;
        }

        private static IDiagnosticLogger? Logger;

        private static readonly Lazy<List<DebugImageInfo>> DebugImagesSorted = new(() =>
        {
            var result = new List<DebugImageInfo>();

            // Only on platforms where we actually use sentry-native.
            if (UnityInfo.IsSupportedBySentryNative(Application.platform))
            {
                var nativeDebugImages = C.DebugImages.Value;
                foreach (var image in nativeDebugImages)
                {
                    if (image.ImageSize is null)
                    {
                        Logger?.Log(SentryLevel.Debug,
                            "Skipping debug image '{0}' (CodeId {1} | DebugId: {2}) because its size is NULL",
                            null, image.CodeFile, image.CodeId, image.DebugId);
                        continue;
                    }

                    var info = new DebugImageInfo(image);
                    var i = 0;
                    for (; i < result.Count; i++)
                    {
                        if (info.StartAddress < result[i].StartAddress)
                        {
                            // insert at index `i`, all the rest have a larger start address
                            break;
                        }
                    }
                    result.Insert(i, info);

                    Logger?.Log(SentryLevel.Debug,
                        "Found debug image '{0}' (CodeId {1} | DebugId: {2}) with addresses between {3:X8} and {4:X8}",
                        null, image.CodeFile, image.CodeId, image.DebugId, info.StartAddress, info.EndAddress);
                }
            }
            return result;
        });

        private static DebugImage? FindDebugImageContainingAddress(ulong instructionAddress)
        {
            var list = DebugImagesSorted.Value;

            // Manual binary search implementation on "value in range".
            var lowerBound = 0;
            var upperBound = list.Count - 1;
            while (lowerBound <= upperBound)
            {
                var mid = (lowerBound + upperBound) / 2;
                var info = list[mid];

                if (info.StartAddress <= instructionAddress)
                {
                    if (instructionAddress <= info.EndAddress)
                    {
                        return info.Image;
                    }
                    lowerBound = mid + 1;
                }
                else
                {
                    upperBound = mid - 1;
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

        private NativeStackTrace GetNativeStackTrace(Exception? e = null)
        {
            // Create a `GCHandle` for the exception (if any), which we can then use to
            // essentially get a pointer to the underlying `Il2CppException` C++ object.
            GCHandle gch = default;

            // The following code allocates and writes the native
            // instruction pointers to the `addresses`/`numFrames` out-parameters.
            var addresses = IntPtr.Zero;
            try
            {
                var numFrames = 0;
                string? imageUuid = null;
                string? imageName = null;

                if (e != null)
                {
                    gch = GCHandle.Alloc(e);

                    var gchandle = GCHandle.ToIntPtr(gch);
                    var addr = _il2CppMethods.Il2CppGcHandleGetTarget(gchandle);

                    _il2CppMethods.Il2CppNativeStackTrace(addr, out addresses, out numFrames, out imageUuid, out imageName);
                }
                else
                {
                    _il2CppMethods.Il2CppNativeStackTraceCurrentThread(out addresses, out numFrames, out imageUuid, out imageName);
                }

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
                if (gch.IsAllocated)
                {
                    // We are done with the `GCHandle`.
                    gch.Free();
                }

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
