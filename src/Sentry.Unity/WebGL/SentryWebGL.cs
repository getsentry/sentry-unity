using System;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;

namespace Sentry.Unity.WebGL
{
    /// <summary>
    /// Configure Sentry for WebGL
    /// </summary>
    public static class SentryWebGL
    {
        /// <summary>
        /// Configures the WebGL support.
        /// </summary>
        /// <param name="options">The Sentry Unity options to use.</param>
        public static void Configure(SentryUnityOptions options)
        {
            // Caching transport relies on a background thread
            options.CacheDirectoryPath = null;
            options.BackgroundWorker = new WebBackgroundWorker(options);

            // Still cant' find out what's using Threads so:
            options.AutoSessionTracking = false;
            options.DetectStartupTime = StartupTimeDetectionMode.None;
            options.DisableTaskUnobservedTaskExceptionCapture();
            options.DisableAppDomainUnhandledExceptionCapture();
            options.DisableAppDomainProcessExitFlush();
            options.DisableDuplicateEventDetection();
            options.ReportAssembliesMode = ReportAssembliesMode.None;
        }
    }

    internal class WebBackgroundWorker : IBackgroundWorker
    {
        private readonly SentryUnityOptions _options;
        private readonly ITransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options)
        {
            _options = options;
            var composer = new SdkComposer(options);
            // HTTP transport is not compatible. Need to use Unity's one.
            _transport = composer.CreateTransport();
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _transport.SendEnvelopeAsync(envelope, CancellationToken.None)
                .ContinueWith(r => _options.DiagnosticLogger?.LogInfo("Result of envelope capture was: {0}", r.Status));
            return true;
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        public int QueuedItems { get; }
    }
}
