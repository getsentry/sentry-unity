using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Internal.Http;
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
            // Note: we need to use a custom background worker which actually doesn't work in the background
            // because Unity doesn't support async (multithreading) yet. This may change in the future so let's watch
            // https://docs.unity3d.com/2019.4/Documentation/ScriptReference/PlayerSettings.WebGL-threadsSupport.html
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
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _ = _behaviour.StartCoroutine(SendEnvelope(envelope));
            return true;
        }

        private IEnumerator SendEnvelope(Envelope envelope)
        {
            var builder = new HttpRequestBuilder(_options);
            var www = new UnityWebRequest();
            www.url = builder.GetEnvelopeEndpointUri().ToString();
            www.method = UnityWebRequest.kHttpVerbPOST;
            www.SetRequestHeader(builder.AuthHeaderName, builder.AuthHeader(_clock.GetUtcNow()));
            // TODO is it OK to call .Wait() here in webGL?
            var stream = new MemoryStream();
            envelope.SerializeAsync(stream, _options.DiagnosticLogger).Wait(TimeSpan.FromSeconds(2));
            stream.Flush();
            www.uploadHandler = new UploadHandlerRaw(stream.ToArray());
            www.downloadHandler = new DownloadHandlerBuffer();
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError || www.responseCode != 200)
            {
                _options.DiagnosticLogger?.LogWarning("error sending request to Sentry: {0}", www.error);
            }

            _options.DiagnosticLogger?.LogDebug("Sentry sent back: {0}", www.downloadHandler.text);
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        public int QueuedItems { get; }
    }
}
