using System;
using System.IO;
using System.Runtime.InteropServices;
using Sentry.Extensibility;
using AOT;
using System.Threading;
using System.Text;

namespace Sentry.Unity
{
    /// <summary>
    /// P/Invoke to `sentry-native` functions.
    /// </summary>
    /// <remarks>
    /// The `sentry-native` SDK on Android is brought in through the `sentry-android-ndk`
    /// maven package.
    /// On Standalone players on Windows and Linux it's build directly for those platforms.
    /// </remarks>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    /// <see href="https://github.com/getsentry/sentry-native"/>
    public static class SentryNativeBridge
    {
        public static void Init(SentryUnityOptions options)
        {
            var cOptions = sentry_options_new();

            // Note: DSN is not null because options.Validate() must have returned true for this to be called.
            sentry_options_set_dsn(cOptions, options.Dsn!);

            if (options.Release is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Release: {0}", options.Release);
                sentry_options_set_release(cOptions, options.Release);
            }

            if (options.Environment is not null)
            {
                options.DiagnosticLogger?.LogDebug("Setting Environment: {0}", options.Environment);
                sentry_options_set_environment(cOptions, options.Environment);
            }

            options.DiagnosticLogger?.LogDebug("Setting Debug: {0}", options.Debug);
            sentry_options_set_debug(cOptions, options.Debug ? 1 : 0);

            if (options.SampleRate.HasValue)
            {
                options.DiagnosticLogger?.LogDebug("Setting Sample Rate: {0}", options.SampleRate.Value);
                sentry_options_set_sample_rate(cOptions, options.SampleRate.Value);
            }

            // Disabling the native in favor of the C# layer for now
            options.DiagnosticLogger?.LogDebug("Disabling native auto session tracking");
            sentry_options_set_auto_session_tracking(cOptions, 0);

            if (options.CacheDirectoryPath is not null)
            {
                var dir = Path.Combine(options.CacheDirectoryPath, "SentryNative");
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    options.DiagnosticLogger?.LogDebug("Setting CacheDirectoryPath on Windows: {0}", dir);
                    sentry_options_set_database_pathw(cOptions, dir);
                }
                else
                {
                    options.DiagnosticLogger?.LogDebug("Setting CacheDirectoryPath: {0}", dir);
                    sentry_options_set_database_path(cOptions, dir);
                }
            }

            if (options.DiagnosticLogger is null)
            {
                _logger?.LogDebug("Unsetting the current native logger");
                _logger = null;
            }
            else
            {
                options.DiagnosticLogger.LogDebug($"{(_logger is null ? "Setting a" : "Replacing the")} native logger");
                _logger = options.DiagnosticLogger;
                sentry_options_set_logger(cOptions, new sentry_logger_function_t(nativeLog), IntPtr.Zero);
            }

            sentry_init(cOptions);
        }

        // libsentry.so
        [DllImport("sentry")]
        private static extern IntPtr sentry_options_new();

        [DllImport("sentry")]
        private static extern void sentry_options_set_dsn(IntPtr options, string dsn);

        [DllImport("sentry")]
        private static extern void sentry_options_set_release(IntPtr options, string release);

        [DllImport("sentry")]
        private static extern void sentry_options_set_debug(IntPtr options, int debug);

        [DllImport("sentry")]
        private static extern void sentry_options_set_environment(IntPtr options, string environment);

        [DllImport("sentry")]
        private static extern void sentry_options_set_sample_rate(IntPtr options, double rate);

        [DllImport("sentry")]
        private static extern void sentry_options_set_database_path(IntPtr options, string path);

        [DllImport("sentry")]
        private static extern void sentry_options_set_database_pathw(IntPtr options, [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport("sentry")]
        private static extern void sentry_options_set_auto_session_tracking(IntPtr options, int debug);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        private delegate void sentry_logger_function_t(int level, string message, IntPtr argsAddress, IntPtr userData);

        [DllImport("sentry")]
        private static extern void sentry_options_set_logger(IntPtr options, sentry_logger_function_t logger, IntPtr userData);

        // The logger we should forward native messages to.
        private static IDiagnosticLogger? _logger;

        // This method is called from the C library
        [MonoPInvokeCallback(typeof(sentry_logger_function_t))]
        private static void nativeLog(int cLevel, string message, IntPtr args, IntPtr userData)
        {
            var logger = _logger;
            if (logger is null)
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

            if (message.Contains("%"))
            {
                var formattedLength = vsnprintf(null, UIntPtr.Zero, message, args);
                var buffer = new StringBuilder(formattedLength + 1);
                vsprintf(buffer, message, args);
                message = buffer.ToString();
            }
            logger.Log(level, $"Native: {message}");
        }

        [DllImport("msvcrt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vsprintf(StringBuilder buffer, string format, IntPtr args);

        [DllImport("msvcrt.dll", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.Cdecl)]
        private static extern int vsnprintf(string? buffer, UIntPtr bufferSize, string format, IntPtr args);

        [DllImport("sentry")]
        private static extern void sentry_init(IntPtr options);

        /// <summary>
        /// Re-installs the sentry-native backend essentially retaking the signal handlers.
        /// </summary>
        /// <summary>
        /// Sentry's Android SDK initializes before Unity. But once Unity initializes, it takes the signal handler
        /// and does not forward the call to the original handler (sentry) before shutting down.
        /// This results in a crash captured by the Sentry Android SDK (Java/ART) layer captured crash report
        /// containing a tombstone, without any of the scope data such as tags set through
        /// Sentry SDKs C# -> Java -> C
        /// </summary>
        public static void ReinstallBackend() => ReinstallSentryNativeBackendStrategy();

        // libsentry.so
        [DllImport("sentry")]
        private static extern void sentry_reinstall_backend();

        // Testing
        internal static Action ReinstallSentryNativeBackendStrategy = sentry_reinstall_backend;
    }
}
