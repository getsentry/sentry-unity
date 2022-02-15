#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#endif
#endif

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#endif

#if UNITY_IOS
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
#endif

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

public class SmokeTester : MonoBehaviour
{
    public void Start()
    {
#if SENTRY_NATIVE_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            var text = intent.Call<String> ("getStringExtra", "test");
            if (text == "smoke")
            {
                SmokeTest();
            }
        }
#elif UNITY_IOS
        string pListPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), "Info.plist");
        using (var streamReader = new StreamReader(pListPath, true))
        {
            var rawPlist = streamReader.ReadToEnd();
            var key = "RunSentrySmokeTest";
            if (rawPlist.Contains(key))
            {
                Debug.Log("Key " + key + " found on Info.plistm starting Smoke test.");
                SmokeTest();
            }
            else
            {
                Debug.Log("To run Smoke Test, please add key " + key + " into Info.plist");
            }
        }
#else
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 2 && args[1] == "--test")
        {
            if (args[2] == "smoke")
            {
                SmokeTest();
            }
        }
#endif
    }

    public static void SmokeTest()
    {
        try
        {
            Debug.Log("SMOKE TEST: Start");
            var evt = new ManualResetEventSlim();

            var requests = new List<string>();
            void Verify(HttpRequestMessage message)
            {
                Debug.Log("SMOKE TEST: Verify invoked.");
                requests.Add(message.Content.ReadAsStringAsync().Result);
                evt.Set();
            }

            var options = new SentryUnityOptions();
            options.Dsn = "https://key@sentry/project";
            options.Debug = true;
            // TODO: Must be set explicitly for the time being.
            options.RequestBodyCompressionLevel = CompressionLevelWithAuto.Auto;
            options.DiagnosticLogger = new ConsoleDiagnosticLogger(SentryLevel.Debug);
            options.CreateHttpClientHandler = () => new TestHandler(Verify);

            var sentryUnityInfo = new SentryUnityInfo();

#if SENTRY_NATIVE_IOS
            Debug.Log("SMOKE TEST: Configure Native iOS.");
            SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
            Debug.Log("SMOKE TEST: Configure Native Android.");
            SentryNativeAndroid.Configure(options, sentryUnityInfo);
#endif

            Debug.Log("SMOKE TEST: SentryUnity Init.");
            SentryUnity.Init(options);

            Debug.Log("SMOKE TEST: SentryUnity Init OK.");

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

            // On Android we'll grep logcat for this string instead of relying on exit code:
            Debug.Log("SMOKE TEST: PASS");

            // Test passed: Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
            Application.Quit(200);

        }
        catch (Exception ex)
        {
            Debug.Log("SMOKE TEST: FAILED");
            Debug.LogError(ex);
            Application.Quit(-1);
        }
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
