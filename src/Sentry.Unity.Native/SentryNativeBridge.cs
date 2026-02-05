using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using UnityEngine;
using AOT;

namespace Sentry.Unity.Native;

/// <summary>
/// P/Invoke to `sentry-native` functions.
/// </summary>
/// <see href="https://github.com/getsentry/sentry-native"/>
internal static class SentryNativeBridge
{
#if SENTRY_NATIVE_SWITCH
    private const string SentryLib = "__Internal";
#else
    private const string SentryLib = "sentry";
#endif

    private static IDiagnosticLogger? Logger; // This is also the logger we're forwarding native messages to.
    private static bool UseLibC;
    private static bool IsWindows;

    public static bool Init(SentryUnityOptions options)
    {
        Logger = options.DiagnosticLogger;

        UseLibC = Application.platform
            is RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxServer
            or RuntimePlatform.PS5 or RuntimePlatform.Switch;
        IsWindows = Application.platform is RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsServer;

        var cOptions = sentry_options_new();

        // Note: DSN is not null because options.IsValid() must have returned true for this to be called.
        sentry_options_set_dsn(cOptions, options.Dsn!);

        if (options.Release is not null)
        {
            Logger?.LogDebug("Setting Release: {0}", options.Release);
            sentry_options_set_release(cOptions, options.Release);
        }

        if (options.Environment is not null)
        {
            Logger?.LogDebug("Setting Environment: {0}", options.Environment);
            sentry_options_set_environment(cOptions, options.Environment);
        }

        Logger?.LogDebug("Setting Debug: {0}", options.Debug);
        sentry_options_set_debug(cOptions, options.Debug ? 1 : 0);

        if (options.SampleRate.HasValue)
        {
            Logger?.LogDebug("Setting Sample Rate: {0}", options.SampleRate.Value);
            sentry_options_set_sample_rate(cOptions, options.SampleRate.Value);
        }

        // Disabling the native in favor of the C# layer for now
        Logger?.LogDebug("Disabling native auto session tracking");
        sentry_options_set_auto_session_tracking(cOptions, 0);

        if (IsWindows)
        {
            Logger?.LogDebug("Setting AttachScreenshot: {0}", options.AttachScreenshot);
            sentry_options_set_attach_screenshot(cOptions, options.AttachScreenshot ? 1 : 0);
        }

        var dir = GetCacheDirectory(options);
#if SENTRY_NATIVE_SWITCH
        Logger?.LogDebug("Setting CacheDirectoryPath: {0}", dir);
        sentry_options_set_database_path(cOptions, dir);
#else
        // Note: don't use RuntimeInformation.IsOSPlatform - it will report windows on WSL.
        if (IsWindows)
        {
            Logger?.LogDebug("Setting CacheDirectoryPath on Windows: {0}", dir);
            sentry_options_set_database_pathw(cOptions, dir);
        }
        else
        {
            Logger?.LogDebug("Setting CacheDirectoryPath: {0}", dir);
            sentry_options_set_database_path(cOptions, dir);
        }
#endif

        if (options.UnityInfo.IL2CPP)
        {
            Logger?.LogDebug("Setting the native logger");
            sentry_options_set_logger(cOptions, new sentry_logger_function_t(nativeLog), IntPtr.Zero);
        }
        else
        {
            Logger?.LogInfo("Passing the native logs back to the C# layer is not supported on Mono - skipping native logger.");
        }

        Logger?.LogDebug("Initializing sentry native");
        return 0 == sentry_init(cOptions);
    }

    public static void Close() => sentry_close();

    // Call after native init() to check if the application has crashed in the previous run and clear the status.
    // Because the file is removed, the result will change on subsequent calls so it must be cached for the current runtime.
    internal static bool HandleCrashedLastRun(SentryUnityOptions options)
    {
        var result = sentry_get_crashed_last_run() == 1;
        sentry_clear_crashed_last_run();
        return result;
    }

    internal static string GetCacheDirectory(SentryUnityOptions options)
    {
        if (options.CacheDirectoryPath is null)
        {
            // same as the default of sentry-native
            return Path.Combine(Directory.GetCurrentDirectory(), ".sentry-native");
        }
        else
        {
            return Path.Combine(options.CacheDirectoryPath, "SentryNative");
        }
    }

    internal static void ReinstallBackend() => sentry_reinstall_backend();

    // libsentry.so
    [DllImport(SentryLib)]
    private static extern IntPtr sentry_options_new();

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_dsn(IntPtr options, string dsn);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_release(IntPtr options, string release);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_debug(IntPtr options, int debug);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_environment(IntPtr options, string environment);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_sample_rate(IntPtr options, double rate);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_database_path(IntPtr options, string path);

#if !SENTRY_NATIVE_SWITCH
    [DllImport(SentryLib)]
    private static extern void sentry_options_set_database_pathw(IntPtr options, [MarshalAs(UnmanagedType.LPWStr)] string path);
#endif

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_auto_session_tracking(IntPtr options, int debug);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_attach_screenshot(IntPtr options, int attachScreenshot);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
    private delegate void sentry_logger_function_t(int level, IntPtr message, IntPtr argsAddress, IntPtr userData);

    [DllImport(SentryLib)]
    private static extern void sentry_options_set_logger(IntPtr options, sentry_logger_function_t logger, IntPtr userData);

    // This method is called from the C library and forwards incoming messages to the currently set _logger.
    [MonoPInvokeCallback(typeof(sentry_logger_function_t))]
    private static void nativeLog(int cLevel, IntPtr format, IntPtr args, IntPtr userData)
    {
        try
        {
            nativeLogImpl(cLevel, format, args, userData);
        }
        catch
        {
            // never allow an exception back to native code - it would crash the app
        }
    }

    private static void nativeLogImpl(int cLevel, IntPtr format, IntPtr args, IntPtr userData)
    {
        var logger = Logger;
        if (logger is null || format == IntPtr.Zero || args == IntPtr.Zero)
        {
            return;
        }

        // see sentry.h: sentry_level_e
        var level = cLevel switch
        {
            -1 => SentryLevel.Debug,
            0 => SentryLevel.Info,
            1 => SentryLevel.Warning,
            2 => SentryLevel.Error,
            3 => SentryLevel.Fatal,
            _ => SentryLevel.Info,
        };

        if (!logger.IsEnabled(level))
        {
            return;
        }

        string? message = null;
        try
        {
            // We cannot access C var-arg (va_list) in c# thus we pass it back to vsnprintf to do the formatting.
            // For Linux and PlayStation, we must make a copy of the VaList to be able to pass it back...
            if (UseLibC)
            {
                var argsStruct = Marshal.PtrToStructure<VaListLinux64>(args);
                var formattedLength = 0;

                WithMarshalledStruct(argsStruct, argsPtr =>
                {
                    formattedLength = 1 + vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, argsPtr);
                });

                WithAllocatedPtr(formattedLength, buffer =>
                    WithMarshalledStruct(argsStruct, argsPtr =>
                    {
                        vsnprintf(buffer, (UIntPtr)formattedLength, format, argsPtr);
                        message = Marshal.PtrToStringAnsi(buffer);
                    }));
            }
            else
            {
                var formattedLength = 1 + vsnprintf(IntPtr.Zero, UIntPtr.Zero, format, args);
                WithAllocatedPtr(formattedLength, buffer =>
                {
                    vsnprintf(buffer, (UIntPtr)formattedLength, format, args);
                    message = Marshal.PtrToStringAnsi(buffer);
                });
            }
        }
        catch (Exception err)
        {
            logger.LogError(err, "Exception in native log forwarder.");
        }

        if (message == null)
        {
            // try to at least print the unreplaced message pattern
            message = Marshal.PtrToStringAnsi(format);
        }

        if (message != null)
        {
            logger.Log(level, $"Native: {message}");
        }
    }

#if SENTRY_NATIVE_PLAYSTATION || SENTRY_NATIVE_SWITCH
    [DllImport("__Internal", EntryPoint = "vsnprintf_sentry")]
    private static extern int vsnprintf_sentry(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args);
#else
    // For Windows/Linux: use platform's native C library directly
    [DllImport("msvcrt", EntryPoint = "vsnprintf")]
    private static extern int vsnprintf_windows(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args);

    [DllImport("libc", EntryPoint = "vsnprintf")]
    private static extern int vsnprintf_linux(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args);
#endif

    private static int vsnprintf(IntPtr buffer, UIntPtr bufferSize, IntPtr format, IntPtr args)
    {
#if SENTRY_NATIVE_PLAYSTATION || SENTRY_NATIVE_SWITCH
        return vsnprintf_sentry(buffer, bufferSize, format, args);
#else
        return IsWindows
            ? vsnprintf_windows(buffer, bufferSize, format, args)
            : vsnprintf_linux(buffer, bufferSize, format, args);
#endif
    }

    // https://stackoverflow.com/a/4958507/2386130
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct VaListLinux64
    {
        uint gp_offset;
        uint fp_offset;
        IntPtr overflow_arg_area;
        IntPtr reg_save_area;
    }

    private static void WithAllocatedPtr(int size, Action<IntPtr> action)
    {
        var ptr = IntPtr.Zero;
        try
        {
            ptr = Marshal.AllocHGlobal(size);
            action(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static void WithMarshalledStruct<T>(T structure, Action<IntPtr> action) where T : notnull =>
        WithAllocatedPtr(Marshal.SizeOf(structure), ptr =>
        {
            Marshal.StructureToPtr(structure, ptr, false);
            action(ptr);
        });

    [DllImport(SentryLib)]
    private static extern int sentry_init(IntPtr options);

    [DllImport(SentryLib)]
    private static extern int sentry_close();

    [DllImport(SentryLib)]
    private static extern int sentry_get_crashed_last_run();

    [DllImport(SentryLib)]
    private static extern int sentry_clear_crashed_last_run();

    [DllImport(SentryLib)]
    private static extern void sentry_reinstall_backend();
}
