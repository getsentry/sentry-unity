using System;
using System.IO;
using Sentry;
using Sentry.Extensibility;
using UnityEngine;

/// <summary>
/// Unified logger for integration tests and the Sentry SDK.
///
/// On Xbox master (non-development) builds, Debug.Log output is suppressed entirely.
/// This class writes directly to a file via StreamWriter, bypassing Unity's logger
/// so that test output (EVENT_CAPTURED lines, status messages) and Sentry SDK
/// diagnostic messages all end up in a retrievable file.
///
/// On other platforms, messages go through Debug.Log as usual.
///
/// Implements <see cref="IDiagnosticLogger"/> so it can be assigned to
/// <c>options.DiagnosticLogger</c>, routing SDK diagnostic output through the same
/// log file without needing a separate logger.
/// </summary>
public class Logger : IDiagnosticLogger
{
    private static StreamWriter s_writer;
    private static readonly object s_lock = new();
    private static string s_logFilePath;
    private SentryLevel _minLevel = SentryLevel.Debug;

    // Instance must be declared after s_lock — static fields initialize in textual order.
    public static readonly Logger Instance = CreateInstance();

    private static Logger CreateInstance()
    {
#if UNITY_GAMECORE && !UNITY_EDITOR
        Open(Path.Combine(@"D:\Logs", "UnityIntegrationTest.log"));
#endif
        return new Logger();
    }

    /// <summary>
    /// Opens the log file. Call once during initialization.
    /// Throws if the file cannot be created — the caller should let the app crash
    /// so the test harness can detect the non-zero exit code.
    /// </summary>
    private static void Open(string logFilePath)
    {
        lock (s_lock)
        {
            if (s_writer != null)
            {
                return;
            }

            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            s_writer = new StreamWriter(logFilePath, append: false) { AutoFlush = true };
            s_logFilePath = logFilePath;
        }
    }

    /// <summary>
    /// Returns the path that was opened, or null if not opened.
    /// </summary>
    public static string GetLogFilePath()
    {
        return s_logFilePath;
    }

    /// <summary>
    /// Writes a line to the log file and Debug.Log.
    /// Safe to call even if the file was never opened — the message still goes to Debug.Log.
    /// </summary>
    public static void Log(string message)
    {
        Debug.Log(message);
        WriteToFile(message);
    }

    /// <summary>
    /// Writes a warning to the log file and Debug.LogWarning.
    /// </summary>
    public static void LogWarning(string message)
    {
        Debug.LogWarning(message);
        WriteToFile($"[WARNING] {message}");
    }

    /// <summary>
    /// Writes an error to the log file and Debug.LogError.
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
        WriteToFile($"[ERROR] {message}");
    }

    private static void WriteToFile(string message)
    {
        lock (s_lock)
        {
            if (s_writer == null)
            {
                return;
            }

            try
            {
                s_writer.WriteLine(message);
            }
            catch
            {
                // Don't let file writing errors break the app.
            }
        }
    }

    // --- IDiagnosticLogger (explicit implementation to avoid collision with static Log) ---

    bool IDiagnosticLogger.IsEnabled(SentryLevel level) => level >= _minLevel;

    void IDiagnosticLogger.Log(SentryLevel logLevel, string message, Exception exception, params object[] args)
    {
        if (!((IDiagnosticLogger)this).IsEnabled(logLevel))
        {
            return;
        }

        var formatted = args?.Length > 0 ? string.Format(message, args) : message;
        if (exception != null)
        {
            formatted = $"{formatted} {exception}";
        }

        var line = $"[Sentry] ({logLevel}) {formatted}";

        switch (logLevel)
        {
            case SentryLevel.Error:
            case SentryLevel.Fatal:
                LogError(line);
                break;
            case SentryLevel.Warning:
                LogWarning(line);
                break;
            default:
                Log(line);
                break;
        }
    }
}
