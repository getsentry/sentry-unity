using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Sentry.Unity.Tests
{
    public class TestHttpClientHandler : HttpClientHandler
    {
        private const string EventQualifier = "\"type\":\"event\"";

        private readonly List<string> _requests = new();
        private readonly AutoResetEvent _requestReceived = new(false);

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

        public string GetEvent(TimeSpan timeout)
        {
            // Check all the already received requests
            lock (_requests)
            {
                var eventRequest = _requests.Find(r => r.Contains(EventQualifier));
                if (!string.IsNullOrEmpty(eventRequest))
                {
                    Debug.Log(UnityLogger.LogPrefix + "TestHttpClientHandler returns event: \n" + eventRequest);
                    return eventRequest;
                }
            }

            // While within timeout: check every newly received request
            var startTime = DateTime.Now;
            while (DateTime.Now < startTime + timeout)
            {
                if (_requestReceived.WaitOne(TimeSpan.FromMilliseconds(16))) // Once per frame
                {
                    lock (_requests)
                    {
                        if (_requests.Count > 0 && _requests[_requests.Count - 1].Contains(EventQualifier))
                        {
                            var eventRequest = _requests[_requests.Count - 1];
                            Debug.Log(UnityLogger.LogPrefix + "TestHttpClientHandler returns event: \n" + eventRequest);

                            return eventRequest;
                        }
                    }
                }
            }

            Debug.LogError("TestHttpClientHandler timed out waiting for an event.");
            return string.Empty;
        }
    }
}
