using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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

            options.CrashedLastRun = () => false; // no way to recognize crashes in WebGL yet

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
        private readonly SentryMonoBehaviour _behaviour;
        private readonly UnityWebRequestTransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _transport = new UnityWebRequestTransport(options, behaviour);
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _ = _behaviour.StartCoroutine(_transport.SendEnvelopeAsync(envelope));
            return true;
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask; // TODO maybe we can implement this somehow?

        public int QueuedItems { get; }
    }

    internal class UnityWebRequestTransport : HttpTransport
    {
        private readonly SentryUnityOptions _options;

        public UnityWebRequestTransport(SentryUnityOptions options, SentryMonoBehaviour behaviour)
            : base(options, new HttpClient(new UnityWebRequestMessageHandler()))
        {
            _options = options;
        }

        // adapted HttpTransport.SendEnvelopeAsync()
        internal IEnumerator SendEnvelopeAsync(Envelope envelope)
        {
            var instant = DateTimeOffset.Now;

            // Apply rate limiting and re-package envelope items
            using var processedEnvelope = ProcessEnvelope(envelope, instant);
            if (processedEnvelope.Items.Count != 0)
            {
                // Send envelope to ingress
                var www = CreateWebRequest(CreateRequest(processedEnvelope));
                yield return www.SendWebRequest();

                var response = GetResponse(www);
                if (response != null)
                {
                    // Read & set rate limits for future requests
                    ExtractRateLimits(response, instant);

                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        HandleFailure(response, processedEnvelope, www);
                    }
                    else if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
                    {
                        _options.DiagnosticLogger?.LogDebug("Envelope '{0}' sent successfully. Payload:\n{1}",
                            envelope.TryGetEventId(), Encoding.UTF8.GetString(www.uploadHandler.data));
                    }
                    else
                    {
                        _options.DiagnosticLogger?.LogInfo("Envelope '{0}' successfully received by Sentry.",
                            processedEnvelope.TryGetEventId());
                    }
                }
            }
        }

        // adapted HttpTransport.HandleFailureAsync()
        private void HandleFailure(HttpResponseMessage response, Envelope processedEnvelope, UnityWebRequest www)
        {
            // Spare the overhead if level is not enabled
            if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Error) is true && response.Content is { } content)
            {
                var responseString = ((ExposedStringContent)response.Content).Content;
                if (string.Equals(content.Headers.ContentType?.MediaType, "application/json",
                    StringComparison.OrdinalIgnoreCase))
                {
                    using var document = JsonDocument.Parse(responseString);
                    LogFailure(response, processedEnvelope, document.RootElement);
                }
                else
                {
                    LogFailure(response, processedEnvelope, responseString);
                }

                // If debug level, dump the whole envelope to the logger
                if (_options.DiagnosticLogger?.IsEnabled(SentryLevel.Debug) is true)
                {
                    _options.DiagnosticLogger?.LogDebug("Failed envelope '{0}' has payload:\n{1}\n",
                        processedEnvelope.TryGetEventId(), Encoding.UTF8.GetString(www.uploadHandler.data));
                }
            }

            // SDK is in debug mode, and envelope was too large. To help troubleshoot:
            // NOTE: likely no point to do this on WebGL - who would check the file (in IndexDB)?
        }

        private UnityWebRequest CreateWebRequest(HttpRequestMessage message)
        {
            var www = new UnityWebRequest();
            www.url = message.RequestUri.ToString();
            www.method = message.Method.Method.ToUpperInvariant();

            foreach (var header in message.Headers)
            {
                www.SetRequestHeader(header.Key, string.Join(",", header.Value));
            }

            var stream = new MemoryStream();
            _ = message.Content.CopyToAsync(stream).Wait(2000);
            stream.Flush();
            www.uploadHandler = new UploadHandlerRaw(stream.ToArray());
            www.downloadHandler = new DownloadHandlerBuffer();
            return www;
        }

        private HttpResponseMessage? GetResponse(UnityWebRequest www)
        {

            // if (www.result == UnityWebRequest.Result.ConnectionError) // unity 2021+
            if (www.isNetworkError) // Unity 2019
            {
                _options.DiagnosticLogger?.LogWarning("Failed to send request: {0}", www.error);
                return null;
            }

            var response = new HttpResponseMessage((HttpStatusCode)www.responseCode);
            foreach (var header in www.GetResponseHeaders())
            {
                // Unity would throw if we tried to set content-type or content-length
                if (header.Key != "content-length" && header.Key != "content-type")
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }
            response.Content = new ExposedStringContent(www.downloadHandler.text);
            return response;
        }
    }

    internal class UnityWebRequestMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage _, CancellationToken __)
        {
            // if this throws, see usages of HttpTransport._httpClient
            throw new InvalidOperationException("UnityWebRequestMessageHandler must be unused");
        }
    }

    internal class ExposedStringContent : StringContent
    {
        internal readonly String Content;
        public ExposedStringContent(String data) : base(data) => Content = data;
    }

    internal static class JsonExtensions
    {
        public static JsonElement? GetPropertyOrNull(this JsonElement json, string name)
        {
            if (json.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (json.TryGetProperty(name, out var result) &&
                result.ValueKind is not JsonValueKind.Undefined and not JsonValueKind.Null)
            {
                return result;
            }

            return null;
        }
    }
}
