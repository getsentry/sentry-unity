using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sentry.Unity.Tests.Stubs
{
    public class TestHttpClientHandler : HttpClientHandler
    {
        private readonly Action<HttpRequestMessage> _messageCallback;

        public TestHttpClientHandler(Action<HttpRequestMessage> messageCallback) => _messageCallback = messageCallback;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _messageCallback(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
