
using System;
using System.Collections.Generic;
using System.Net.Http;
using UnityEngine;

namespace Sentry
{
    public class Dsn
    {
        string dsn;
        Uri uri;

        public Uri callUri;
        public string secretKey, publicKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dsn"/> class.
        /// </summary>
        /// <param name="dsn">The DSN in the format: {PROTOCOL}://{PUBLIC_KEY}@{HOST}/{PATH}{PROJECT_ID}</param>
        /// <remarks>
        /// A legacy DSN containing a secret will also be accepted: {PROTOCOL}://{PUBLIC_KEY}:{SECRET_KEY}@{HOST}/{PATH}{PROJECT_ID}
        /// </remarks>
        public Dsn(string dsn)
        {
            this.dsn = dsn;
            uri = new Uri(dsn);
            if (string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            var keys = uri.UserInfo.Split(':');
            publicKey = keys[0];
            if (string.IsNullOrWhiteSpace(publicKey))
            {
                throw new ArgumentException("Invalid DSN: No public key provided.");
            }
            secretKey = null;
            if (keys.Length > 1)
            {
                secretKey = keys[1];
            }

            var path = uri.AbsolutePath.Substring(0, uri.AbsolutePath.LastIndexOf('/'));
            var projectId = uri.AbsoluteUri.Substring(uri.AbsoluteUri.LastIndexOf('/') + 1);

            if (string.IsNullOrWhiteSpace(projectId))
            {
                throw new ArgumentException("Invalid DSN: A Project Id is required.");
            }

            var builder = new UriBuilder
            {
                Scheme = uri.Scheme,
                Host = uri.DnsSafeHost,
                Port = uri.Port,
                Path = $"{path}/api/{projectId}/store/"
            };
            callUri = builder.Uri;

            /*ProjectId = parsed.Item1;
            Path = parsed.Item2;
            SecretKey = parsed.Item3;
            PublicKey = parsed.Item4;
            SentryUri = parsed.Item5;*/
        }
    }    

    public class SentrySdk
    {
        Dsn _dsn;

        public SentrySdk(string dsn)
        {
            _dsn = new Dsn(dsn);
        }

        private long ConvertToTimestamp(DateTime value)
        {
            long epoch = (value.Ticks - 621355968000000000) / 10000000;
            return epoch;
        }

        public class _SentrySdk
        {
            public string name = "sentry-unity";
            public string version = "0.0.1";
        }

        public class SentryMessage
        {
            public string event_id;
            public string message;
            public string timestamp;
            public string logger = "error";
            public string platform = "csharp";
            public _SentrySdk sdkSpec = new _SentrySdk();

            public SentryMessage(string event_id, string message)
            {
                this.event_id = event_id;
                this.message = message;
                this.timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
            }
        }

        public async void sendMessage(string msg)
        {
            var client = new HttpClient();
            var timestamp = ConvertToTimestamp(DateTime.Now);
            var sentryKey = _dsn.publicKey;
            var sentrySecret = _dsn.secretKey;
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Sentry-Auth",
                     $"Sentry sentry_version=5,sentry_client=Unity0.1," +
                     $"sentry_timestamp={timestamp}," +
                     $"sentry_key={sentryKey},sentry_secret={sentrySecret}");

            var values = new Dictionary<string, string>();
            var guid = Guid.NewGuid().ToString("N");
            var content = new StringContent(JsonUtility.ToJson(new SentryMessage(
                guid, "foobar2")));

            var response = await client.PostAsync(_dsn.callUri, content);

            var responseString = await response.Content.ReadAsStringAsync();
            Debug.Log(responseString);
        }
    }
}