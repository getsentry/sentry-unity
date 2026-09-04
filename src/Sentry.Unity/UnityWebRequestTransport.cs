using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Sentry.Extensibility;
using Sentry.Http;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol.Envelopes;
using Sentry.Unity.Integrations;
using UnityEngine;
using UnityEngine.Networking;

namespace Sentry.Unity;

internal class WebBackgroundWorker : IBackgroundWorker
{
    private readonly SentryMonoBehaviour _behaviour;
    private readonly UnityWebRequestTransport _transport;
    private int _pendingItems;

    public WebBackgroundWorker(SentryUnityOptions options, SentryMonoBehaviour behaviour)
    {
        _behaviour = behaviour;
        _transport = new UnityWebRequestTransport(options);
    }

    public bool EnqueueEnvelope(Envelope envelope)
    {
        _pendingItems++;
        _behaviour.QueueCoroutine(SendAndTrack(envelope));
        return true;
    }

    private IEnumerator SendAndTrack(Envelope envelope)
    {
        try
        {
            yield return _transport.SendEnvelopeAsync(envelope);
        }
        finally
        {
            _pendingItems--;
        }
    }

    public Task FlushAsync(TimeSpan timeout) => Task.CompletedTask;

    public int QueuedItems => _pendingItems;
}

internal class UnityWebRequestTransport : HttpTransportBase
{
    private readonly SentryUnityOptions _options;
    private readonly IApplication _application;

    private const double MaxBackoffSeconds = 300.0;
    private double _backoffSeconds;
    private double _retryAfterRealtime;

    public UnityWebRequestTransport(SentryUnityOptions options, IApplication? application = null)
        : base(options)
    {
        _options = options;
        _application = application ?? ApplicationAdapter.Instance;
    }

    // adapted HttpTransport.SendEnvelopeAsync()
    internal IEnumerator SendEnvelopeAsync(Envelope envelope)
    {
        // This transport opens a connection per envelope, causing a prompt to appear on platforms
        // like Nintendo Switch while offline.
        if (!IsNetworkAvailable() || Time.realtimeSinceStartupAsDouble < _retryAfterRealtime)
        {
            _options.LogDebug("Network unavailable or backing off. Dropping envelope instead of attempting to send.");
            _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, envelope);
            yield break;
        }

        using var processedEnvelope = ProcessEnvelope(envelope);
        if (processedEnvelope.Items.Count > 0)
        {
            // Send envelope to ingress
            var httpRequest = CreateRequest(processedEnvelope);
            var www = CreateWebRequest(httpRequest);
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.ConnectionError)
            {
                _backoffSeconds = Math.Min(MaxBackoffSeconds, _backoffSeconds > 0.0 ? _backoffSeconds * 2 : 1.0);
                _retryAfterRealtime = Time.realtimeSinceStartupAsDouble + _backoffSeconds;

                _options.LogWarning("Failed to send request: {0}. Backing off for {1:F1}s.", www.error, _backoffSeconds);
                _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, processedEnvelope);
                yield break;
            }

            _backoffSeconds = 0.0;
            _retryAfterRealtime = 0.0;

            var response = GetResponse(www);
            if (response is not null)
            {
                HandleResponse(response, processedEnvelope);
            }
        }
    }

    private bool IsNetworkAvailable()
    {
        if (_options.NetworkAvailabilityProbe is { } probe)
        {
            return probe();
        }

        return _application.InternetReachability != NetworkReachability.NotReachable;
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
        if (www.result == UnityWebRequest.Result.ConnectionError)
        {
            _options.LogWarning("Failed to send request: {0}", www.error);
            return null;
        }

        var response = new HttpResponseMessage((HttpStatusCode)www.responseCode);
        foreach (var header in www.GetResponseHeaders())
        {
            try
            {
                // Unity would throw if we tried to set content-type, content-length, or content-encoding
                if (!header.Key.StartsWith("content-", StringComparison.InvariantCultureIgnoreCase))
                {
                    response.Headers.Add(header.Key, header.Value);
                }
            }
            catch (InvalidOperationException e)
            {
                _options.LogError(e, "Failed to extract response header: {0}", header.Key);
            }
        }
        response.Content = new StringContent(www.downloadHandler.text);
        return response;
    }
}
