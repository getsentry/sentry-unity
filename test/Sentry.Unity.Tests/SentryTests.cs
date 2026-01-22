using System;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

/// <summary>
/// Shared test utilities for Sentry Unity tests.
/// </summary>
public static class SentryTests
{
    /// <summary>
    /// Default test DSN used across tests.
    /// </summary>
    public const string TestDsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";

    /// <summary>
    /// Creates a properly configured SentryUnityOptions for testing.
    /// Uses a temp directory for cache to avoid polluting the project directory.
    /// </summary>
    public static SentryUnityOptions CreateOptions(Action<SentryUnityOptions>? configure = null)
    {
        var options = new SentryUnityOptions
        {
            // Use temp directory to avoid creating Sentry cache in the project directory
            CacheDirectoryPath = TestApplication.DefaultPersistentDataPath
        };
        configure?.Invoke(options);
        return options;
    }

    /// <summary>
    /// Initializes the Sentry SDK for testing with proper configuration.
    /// Returns a disposable that closes the SDK when disposed.
    /// </summary>
    public static IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null, TestHttpClientHandler? testHttpClientHandler = null)
    {
        SentrySdk.Init(options =>
        {
            options.Dsn = TestDsn;
            // Use temp directory to avoid creating Sentry cache in the project directory
            options.CacheDirectoryPath = TestApplication.DefaultPersistentDataPath;

            if (testHttpClientHandler is not null)
            {
                options.CreateHttpMessageHandler = () => testHttpClientHandler;
            }

            configure?.Invoke(options);
        });

        return new SentryDisposable();
    }

    private sealed class SentryDisposable : IDisposable
    {
        public void Dispose() => SentrySdk.Close();
    }
}
