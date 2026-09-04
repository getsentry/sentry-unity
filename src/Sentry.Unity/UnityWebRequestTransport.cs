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

    // This transport opens a connection per envelope, and on Nintendo Switch every attempt made
    // while offline raises the system's "connect to the internet" dialog. With logs or metrics
    // enabled that is a prompt every few seconds, so sends are gated on network availability and
    // spaced out by a backoff where availability cannot be trusted.
    private const double InitialBackoffSeconds = 1.0;
    private const double MaxBackoffSeconds = 60.0;

    private readonly IApplication _application;

    private int _consecutiveConnectionErrors;
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
        if (!CanAttemptSend(envelope))
        {
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
                OnConnectionError(www, processedEnvelope);
                yield break;
            }

            OnConnectionSucceeded();

            var response = GetResponse(www);
            if (response is not null)
            {
                HandleResponse(response, processedEnvelope);
            }
        }
    }

    /// <summary>
    /// Whether the platform reports a usable network. The platform's own probe wins over
    /// reachability, which on Nintendo Switch claims the console is reachable while it is offline.
    /// </summary>
    private bool IsNetworkAvailable()
    {
        if (_options.NetworkAvailabilityProbe is { } probe)
        {
            return probe();
        }

        return _application.InternetReachability != NetworkReachability.NotReachable;
    }

    /// <summary>
    /// Whether it is worth opening a connection for this envelope. Drops are recorded so client
    /// reports still account for them.
    /// </summary>
    private bool CanAttemptSend(Envelope envelope)
    {
        if (!IsNetworkAvailable())
        {
            _options.LogDebug("No network available. Dropping envelope instead of attempting to send.");
            _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, envelope);
            return false;
        }

        if (_consecutiveConnectionErrors > 0)
        {
            var remaining = _retryAfterRealtime - Time.realtimeSinceStartupAsDouble;
            if (remaining > 0.0)
            {
                _options.LogDebug(
                    "Backing off after {0} failed connection attempt(s). Dropping envelope, retrying in {1:F1}s.",
                    _consecutiveConnectionErrors, remaining);
                _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, envelope);
                return false;
            }
        }

        return true;
    }

    private void OnConnectionError(UnityWebRequest www, Envelope envelope)
    {
        _consecutiveConnectionErrors++;

        var backoff = Math.Min(
            MaxBackoffSeconds,
            InitialBackoffSeconds * Math.Pow(2, _consecutiveConnectionErrors - 1));
        _retryAfterRealtime = Time.realtimeSinceStartupAsDouble + backoff;

        // Reachability is logged because a platform claiming to be reachable while the connection
        // fails is the case the backoff exists to cover.
        var backoffDetail = $"{backoff:F1}s (consecutive failure #{_consecutiveConnectionErrors})";
        _options.LogWarning(
            "Failed to send request: {0}. Reachability reported as {1}. Backing off for {2}.",
            www.error, _application.InternetReachability, backoffDetail);

        _options.ClientReportRecorder.RecordDiscardedEvents(DiscardReason.NetworkError, envelope);
    }

    private void OnConnectionSucceeded()
    {
        if (_consecutiveConnectionErrors == 0)
        {
            return;
        }

        _options.LogDebug("Connection restored after {0} failed attempt(s).", _consecutiveConnectionErrors);
        _consecutiveConnectionErrors = 0;
        _retryAfterRealtime = 0.0;
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
