using System;

namespace Sentry.Unity.Tests
{
    public static class SentryTests
    {
        internal static IDisposable InitSentrySdk(Action<SentryUnityOptions>? configure = null, TestHttpClientHandler? testHttpClientHandler = null)
        {
            SentryUnity.Init(options =>
            {
                options.Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";
                if (testHttpClientHandler is not null)
                {
                    options.CreateHttpClientHandler = () => testHttpClientHandler;
                }

                configure?.Invoke(options);
            });

            return new SentryDisposable();
        }

        private sealed class SentryDisposable : IDisposable
        {
            public void Dispose() => SentrySdk.Close();
        }
    }
}
