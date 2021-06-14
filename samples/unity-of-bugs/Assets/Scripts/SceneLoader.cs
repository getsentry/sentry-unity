using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Sentry;
using Sentry.Infrastructure;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.SceneManagement;
using static System.Environment;

public class SceneLoader : MonoBehaviour
{
    public void LoadBugFarmScene() => SceneManager.LoadScene("1_BugFarmScene");

    public void LoadTransitionScene() => SceneManager.LoadScene("2_TransitionScene");

    public void Start()
    {
        var args = GetCommandLineArgs();
        if (args.Length > 2 && args[1] == "--test")
        {
            if (args[2] == "smoke")
            {
                SmokeTest();
            }
        }
    }

    public static void SmokeTest()
    {
        var evt = new ManualResetEventSlim();

        var requests = new List<string>();
        void Verify(HttpRequestMessage message)
        {
            requests.Add(message.Content.ReadAsStringAsync().Result);
            evt.Set();
        }

        SentryUnity.Init(o =>
        {
            o.Dsn = "https://key@sentry/project";
            o.Debug = true;
            // TODO: Must be set explicitly for the time being.
            o.RequestBodyCompressionLevel = CompressionLevelWithAuto.Auto;
            o.DiagnosticLogger = new ConsoleDiagnosticLogger(SentryLevel.Debug);
            o.CreateHttpClientHandler = () => new TestHandler(Verify);
        });

        var guid = Guid.NewGuid().ToString();
        Debug.LogError(guid);
        SentrySdk.CaptureMessage(guid);

        if (!evt.Wait(TimeSpan.FromSeconds(3)))
        {
            // 1 = timeout
            Application.Quit(1);
        }

        if (!requests.Any(r => r.Contains(guid)))
        {
            // 2 event captured but guid not there.
            Application.Quit(2);
        }

        // Test passed: Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
        Application.Quit(200);
    }

    private class TestHandler : HttpClientHandler
    {
        private readonly Action<HttpRequestMessage> _messageCallback;

        public TestHandler(Action<HttpRequestMessage> messageCallback) => _messageCallback = messageCallback;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            _messageCallback(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
