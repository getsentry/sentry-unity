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

    /// <summary>
    /// Opens the log file. Call once during initialization.
    /// Subsequent calls are ignored if a writer is already open.
    /// </summary>
    public static void Open(string logFilePath)
    {
        lock (s_lock)
        {
            if (s_writer != null)
            {
                return;
            }

            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                s_writer = new StreamWriter(logFilePath, append: false) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                // If we can't write to the file, don't break the app.
                Debug.LogWarning($"Logger: Failed to open '{logFilePath}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Writes a line to the log file and Debug.Log.
    /// Safe to call even if the file was never opened — the message still goes to Debug.Log.
    /// </summary>
    public static void Log(string message)
    {
        // Always attempt Debug.Log — on platforms where it works, this gives us console output.
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
