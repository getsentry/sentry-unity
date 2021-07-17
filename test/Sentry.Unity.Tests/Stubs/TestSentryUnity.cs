using System;
using System.Net.Http;

namespace Sentry.Unity.Tests.Stubs
{
    public static class TestSentryUnity
    {
        internal static IDisposable Init(
            Action<SentryUnityOptions> configure,
            Action<HttpRequestMessage>? httpRequestCallback = null)
        {
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417";
                if (httpRequestCallback is not null)
                {
                    options.CreateHttpClientHandler = () => new TestHttpClientHandler(httpRequestCallback);
                }

                configure.Invoke(options);
            });
            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
    }
}
