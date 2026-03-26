using System;
using System.Collections.Generic;
using System.IO;
using Sentry;
using Sentry.Extensibility;
using Sentry.Unity;
using UnityEngine;

public class IntegrationOptionsConfiguration : SentryOptionsConfiguration
{
    public override void Configure(SentryUnityOptions options)
    {
        Debug.Log("Sentry: IntegrationOptionsConfig::Configure() called");

        // DSN is baked into SentryOptions.asset at build time by configure-sentry.ps1
        // which passes the SENTRY_DSN env var to ConfigureOptions via the -dsn argument.

        options.Environment = "integration-test";
        options.Release = "sentry-unity-test@1.0.0";
        options.Distribution = "test-dist";

        options.AttachScreenshot = true;
        options.Debug = true;
        options.DiagnosticLevel = SentryLevel.Debug;
        options.TracesSampleRate = 1.0d;

#if UNITY_GAMECORE
        // On Xbox, Debug.Log output is suppressed in non-development builds, so SDK diagnostic
        // logs must be written to a known file path for the test harness to retrieve them.
        options.DiagnosticLogger = new SdkFileLogger(
            "D:\\Logs\\sentry-sdk.log",
            options.DiagnosticLevel);
#endif

        // No custom HTTP handler -- events go to real sentry.io

        // Filtering test output from breadcrumbs
        options.AddBreadcrumbsForLogType = new Dictionary<LogType, bool>
        {
            { LogType.Error, true },
            { LogType.Assert, true },
            { LogType.Warning, true },
            { LogType.Log, false },
            { LogType.Exception, true },
        };

        // Disable ANR to avoid test interference
        options.DisableAnrIntegration();

        // Runtime initialization for integration tests
        options.AndroidNativeInitializationType = NativeInitializationType.Runtime;
        options.IosNativeInitializationType = NativeInitializationType.Runtime;

        Debug.Log("Sentry: IntegrationOptionsConfig::Configure() finished");
    }

    private class SdkFileLogger : IDiagnosticLogger
    {
        private readonly StreamWriter _writer;
        private readonly SentryLevel _minLevel;

        public SdkFileLogger(string logFilePath, SentryLevel minLevel)
        {
            _minLevel = minLevel;
            try
            {
                var directory = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                _writer = new StreamWriter(logFilePath, append: true) { AutoFlush = true };
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"SdkFileLogger: Failed to open log file '{logFilePath}': {ex.Message}");
            }
        }

        public bool IsEnabled(SentryLevel level) => level >= _minLevel;

        public void Log(SentryLevel logLevel, string message, Exception? exception = null, params object?[] args)
        {
            if (!IsEnabled(logLevel) || _writer == null)
            {
                return;
            }

            try
            {
                var text = args.Length == 0 ? message : string.Format(message, args);
                var line = exception == null
                    ? $"[Sentry] {logLevel}: {text}"
                    : $"[Sentry] {logLevel}: {text}{Environment.NewLine}{exception}";
                _writer.WriteLine(line);
            }
            catch
            {
                // Don't let file writing errors break the app.
            }
        }
    }
}
