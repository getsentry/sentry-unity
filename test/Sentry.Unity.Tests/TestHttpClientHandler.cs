using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;

namespace Sentry.Unity.Tests;

public class TestHttpClientHandler : HttpClientHandler
{
    private readonly string name;

    private readonly List<string> _requests = new();
    private readonly AutoResetEvent _requestReceived = new(false);

    public TestHttpClientHandler(string name = "TestHttpClientHandler")
    {
        this.name = name;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Receive(request);
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
    }

    private void Receive(HttpRequestMessage message)
    {
        var messageText = message.Content.ReadAsStringAsync().Result;
        lock (_requests)
        {
            _requests.Add(messageText);
            _requestReceived.Set();
        }
    }

    public string GetEvent(string identifier, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            lock (_requests)
            {
                var eventRequest = _requests.Find(r => r.Contains(identifier));
                if (!string.IsNullOrEmpty(eventRequest))
                {
                    Debug.Log($"{UnityLogger.LogTag}{name} returns event:\n" + eventRequest);
                    return eventRequest;
                }
            }

            _requestReceived.WaitOne(TimeSpan.FromMilliseconds(16)); // Once per frame
        }

        Debug.LogError($"{UnityLogger.LogTag}{name} timed out waiting for an event.");
        return string.Empty;
    }
}
