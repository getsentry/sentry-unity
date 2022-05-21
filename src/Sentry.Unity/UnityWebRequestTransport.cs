using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Protocol.Envelopes;
using UnityEngine;
using UnityEngine.Networking;

namespace Sentry.Unity
{
    internal class WebBackgroundWorker : IBackgroundWorker
    {
        private readonly SentryMonoBehaviour _behaviour;
        private readonly UnityWebRequestTransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _transport = new UnityWebRequestTransport(options);
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _ = _behaviour.StartCoroutine(_transport.SendEnvelopeAsync(envelope));
            return true;
        }

        public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

        public int QueuedItems { get; }
    }

    internal class UnityWebRequestTransport : HttpTransportBase
    {
        private readonly SentryUnityOptions _options;

        public UnityWebRequestTransport(SentryUnityOptions options)
            : base(options)
        => _options = options;

        // adapted HttpTransport.SendEnvelopeAsync()
        internal IEnumerator SendEnvelopeAsync(Envelope envelope)
        {
            using var processedEnvelope = ProcessEnvelope(envelope);
            if (processedEnvelope.Items.Count > 0)
            {
                // Send envelope to ingress
                var httpRequest = CreateRequest(processedEnvelope);
                var www = CreateWebRequest(httpRequest);
                yield return www.SendWebRequest();

                var response = GetResponse(www);
                if (response is not null)
                {
                    HandleResponse(response, processedEnvelope);
                }
            }
        }

        private UnityWebRequest CreateWebRequest(HttpRequestMessage message)
        {
            using var contentStream = ReadStreamFromHttpContent(message.Content);
            var contentMemoryStream = contentStream as MemoryStream;
            if (contentMemoryStream is null)
            {
                contentMemoryStream = new MemoryStream();
                contentStream.CopyTo(contentMemoryStream);
                contentMemoryStream.Flush();
            }

            var www = new UnityWebRequest
            {
                url = message.RequestUri.ToString(),
                method = message.Method.Method.ToUpperInvariant(),
                uploadHandler = new UploadHandlerRaw(contentMemoryStream.ToArray()),
                downloadHandler = new DownloadHandlerBuffer()
            };

            foreach (var header in message.Headers)
            {
                www.SetRequestHeader(header.Key, string.Join(",", header.Value));
            }

            return www;
        }

        private HttpResponseMessage? GetResponse(UnityWebRequest www)
        {
            // Let's disable treating "warning:obsolete" as an error here because the alternative of putting a static
            // function to user code (to be able to use #if UNITY_2019) is just ugly.
#pragma warning disable 618
            // if (www.result == UnityWebRequest.Result.ConnectionError) // Unity 2020.1+; `.result` not present on 2019
            if (www.isNetworkError) // Unity 2019; obsolete (error) on later versions
#pragma warning restore 618
            {
                _options.DiagnosticLogger?.LogWarning("Failed to send request: {0}", www.error);
                return null;
            }

            var response = new HttpResponseMessage((HttpStatusCode)www.responseCode);
            foreach (var header in www.GetResponseHeaders())
            {
                // Unity would throw if we tried to set content-type or content-length
                if (header.Key.ToLowerInvariant() is not ("content-length" or "content-type"))
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }
            response.Content = new StringContent(www.downloadHandler.text);
            return response;
        }
    }
}
