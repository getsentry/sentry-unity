using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;
using UnityEngine;
using UnityEngine.Networking;

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
            options.DiagnosticLogger?.LogDebug("Updating configuration for Unity WebGL.");

            // Caching transport relies on a background thread
            options.CacheDirectoryPath = null;
            options.BackgroundWorker = new WebBackgroundWorker(options, SentryMonoBehaviour.Instance);

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
        private readonly ISystemClock _clock = new SystemClock();

        private readonly SentryMonoBehaviour _behaviour;
        // private readonly ITransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options, SentryMonoBehaviour behaviour)
        {
            _options = options;
            _behaviour = behaviour;
            // var composer = new SdkComposer(options);
            // HTTP transport is not compatible. Need to use Unity's one.
            // _transport = composer.CreateTransport();
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            // _transport.SendEnvelopeAsync(envelope, CancellationToken.None)
            //     .ContinueWith(r => _options.DiagnosticLogger?.LogInfo("Result of envelope capture was: {0}", r.Status));
            _ = _behaviour.StartCoroutine(SendEnvelope(envelope));
            return true;
        }

        private IEnumerator SendEnvelope(Envelope envelope)
        {
            var dsn = Dsn.Parse(_options.Dsn!);
            var authHeader =
                $"Sentry sentry_version={Sentry.Constants.ProtocolVersion}," +
                $"sentry_client={UnitySdkInfo.Name}/{UnitySdkInfo.Version}," +
                $"sentry_key={dsn.PublicKey}," +
                (dsn.SecretKey is { } secretKey ? $"sentry_secret={secretKey}," : null) +
                $"sentry_timestamp={_clock.GetUtcNow().ToUnixTimeSeconds()}";

            var www = new UnityWebRequest(dsn.GetEnvelopeEndpointUri());
            www.method = "POST";
            www.SetRequestHeader("X-Sentry-Auth", authHeader);
            var stream = new MemoryStream();
            envelope.SerializeAsync(stream, _options.DiagnosticLogger).Wait(TimeSpan.FromSeconds(2));
            stream.Flush();
            www.uploadHandler = new UploadHandlerRaw(stream.ToArray());
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            while (!www.isDone)
            {
                yield return null;
            }
            if (
                www.isNetworkError || www.isHttpError
                                   || www.responseCode != 200)
            {
                _options.DiagnosticLogger?.LogWarning("error sending request to sentry: {0}", www.error);
            }
            {
                _options.DiagnosticLogger?.LogDebug("Sentry sent back: {0}", www.downloadHandler.text);
            }
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        public int QueuedItems { get; }
    }
}