using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Replaces Unity's default log handler with one that writes to a file on disk.
/// On platforms like Xbox, Debug.Log output is suppressed in non-development builds.
/// This handler ensures all log output is captured to a known file path so the test
/// harness can retrieve and inspect it.
///
/// Activated by passing `-logFile <path>` on the command line.
/// </summary>
public static class FileLogHandler
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        var logFilePath = GetLogFileArg();
        if (string.IsNullOrEmpty(logFilePath))
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

            var writer = new StreamWriter(logFilePath, append: false) { AutoFlush = true };
            var originalHandler = Debug.unityLogger.logHandler;
            Debug.unityLogger.logHandler = new Handler(writer, originalHandler);

            // Can't use Debug.Log here - it would recurse through the handler before it's
            // fully set up in all cases, but actually it's fine since we already assigned it.
            Debug.Log($"FileLogHandler: Writing logs to {logFilePath}");
        }
        catch (Exception ex)
        {
            // If we can't write to the file, don't break the app — just continue without file logging.
            Debug.LogWarning($"FileLogHandler: Failed to initialize log file at '{logFilePath}': {ex.Message}");
        }
    }

    private static string GetLogFileArg()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-logFile")
            {
                return args[i + 1];
            }
        }
        return null;
    }

    private class Handler : ILogHandler
    {
        private readonly StreamWriter _writer;
        private readonly ILogHandler _originalHandler;

        public Handler(StreamWriter writer, ILogHandler originalHandler)
        {
            _writer = writer;
            _originalHandler = originalHandler;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            try
            {
                var message = args.Length > 0 ? string.Format(format, args) : format;
                _writer.WriteLine(message);
            }
            catch
            {
                // Don't let file writing errors break the app.
            }

            _originalHandler.LogFormat(logType, context, format, args);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            try
            {
                _writer.WriteLine(exception.ToString());
            }
            catch
            {
                // Don't let file writing errors break the app.
            }

            _originalHandler.LogException(exception, context);
        }
    }
}
