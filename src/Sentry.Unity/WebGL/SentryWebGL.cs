using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Http;
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
        private readonly HttpTransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options, SentryMonoBehaviour behaviour) =>
            _transport = new HttpTransport(options, new HttpClient(new UnityWebRequestMessageHandler(options, behaviour)));

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _ = _transport.SendEnvelopeAsync(envelope);
            return true;
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask; // TODO maybe we can implement this somehow?

        public int QueuedItems { get; }
    }

    internal class UnityWebRequestMessageHandler : HttpMessageHandler
    {
        private readonly SentryMonoBehaviour _behaviour;
        private readonly SentryOptions _options;

        public UnityWebRequestMessageHandler(SentryOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _options = options;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();
            _ = _behaviour.StartCoroutine(SendInCoroutine(message, cancellationToken, tcs));
            return tcs.Task;
        }

        private IEnumerator SendInCoroutine(HttpRequestMessage message, CancellationToken cancellationToken, TaskCompletionSource<HttpResponseMessage> tcs)
        {
            var www = new UnityWebRequest();
            UnityWebRequestAsyncOperation? result;
            try
            {
                www.url = message.RequestUri.ToString();
                www.method = message.Method.Method.ToUpperInvariant();

                foreach (var header in message.Headers)
                {
                    www.SetRequestHeader(header.Key, string.Join(",", header.Value));
                }

                var stream = new MemoryStream();
                _ = message.Content.CopyToAsync(stream).Wait(2000, cancellationToken);
                stream.Flush();
                www.uploadHandler = new UploadHandlerRaw(stream.ToArray());
                www.downloadHandler = new DownloadHandlerBuffer();
                result = www.SendWebRequest();
            }
            catch (Exception e)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Warning, "Error sending request to Sentry", e);
                _ = tcs.TrySetException(e);
                result = null;
            }
            yield return result;


            if (result != null)
            {
                try
                {
                    if (www.isNetworkError)
                    {
                        throw new Exception(www.error);
                    }
                    else
                    {
                        var response = new HttpResponseMessage((HttpStatusCode)www.responseCode);
                        foreach (var header in www.GetResponseHeaders())
                        {
                            // Unity would throw if we tried to set content-length/content-length
                            if (!string.Equals(header.Key, "content-length", StringComparison.InvariantCultureIgnoreCase)
                                && !string.Equals(header.Key, "content-type", StringComparison.InvariantCultureIgnoreCase))
                            {
                                response.Headers.Add(header.Key, header.Value);
                            }
                        }
                        response.Content = new StringContent(www.downloadHandler.text);
                        _ = tcs.TrySetResult(response);
                    }
                }
                catch (Exception e)
                {
                    _options.DiagnosticLogger?.Log(SentryLevel.Warning, "Error sending request to Sentry", e);
                    _ = tcs.TrySetException(e);
                }
            }
        }
    }
}
