using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Shared log writer for integration tests.
///
/// On Xbox master (non-development) builds, Debug.Log output is suppressed entirely.
/// This class writes directly to a file via StreamWriter, bypassing Unity's logger
/// so that test output (EVENT_CAPTURED lines, status messages) ends up in a
/// retrievable file.
///
/// On other platforms, messages go through Debug.Log as usual.
/// </summary>
public static class Logger
{
    private static StreamWriter s_writer;
    private static readonly object s_lock = new();
    private static string s_logFilePath;

    /// <summary>
    /// Opens the log file. Call once during initialization.
    /// Throws if the file cannot be created — the caller should let the app crash
    /// so the test harness can detect the non-zero exit code.
    /// </summary>
    public static void Open(string logFilePath)
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
}
