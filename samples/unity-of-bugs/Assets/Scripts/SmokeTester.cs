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
        var exitCode = 0;
        void Exit(int code)
        {
            if (exitCode != 0)
            {
                Debug.Log($"Ignoring spurious Exit({code}). Application is already exiting with code {exitCode}");
            }
            else
            {
                exitCode = code;
                Application.Quit(code);
                // Application.Quit doesn't actually terminate immediately so exit the context at least...
                throw new Exception($"Quitting with exit code {code}");
            }
        }

        try
        {
            Debug.Log("SMOKE TEST: Start");
            var evt = new AutoResetEvent(false);

            var requests = new List<string>();
            void Verify(HttpRequestMessage message)
            {
                var msgText = message.Content.ReadAsStringAsync().Result;
                Debug.Log($"SMOKE TEST: Intercepted HTTP Request #{requests.Count} = {msgText}");
                requests.Add(msgText);
                evt.Set();
            }

            var testNumber = 0;
            void Expect(String message, bool result)
            {
                testNumber++;
                Debug.Log($"SMOKE TEST | {testNumber}. {message}: {(result ? "PASS" : "FAIL")}");
                if (!result)
                {
                    Debug.Log($"SMOKE TEST: Quitting due to a failed test case #{testNumber}");
                    Exit(testNumber);
                }
            }

            void ExpectMessage(int index, String substring)
            {
                Debug.Log($"SMOKE TEST: Checking if the HTTP request #{index} contains: '{substring}'.");
                while (requests.Count < index + 1)
                {
                    if (!evt.WaitOne(TimeSpan.FromSeconds(3)))
                    {
                        Debug.Log($"SMOKE TEST: Failed while waiting for an HTTP request #{index} to come in.");
                        Exit(testNumber);
                    }
                }
                Expect($"HTTP Request #{index} contains '{substring}'.", requests[index].Contains(substring));
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

            ExpectMessage(0, "\"type\":\"session\"");
            ExpectMessage(0, "\"init\":false");

            ExpectMessage(1, "\"type\":\"session\"");
            ExpectMessage(1, "\"init\":true");

            var guid = Guid.NewGuid().ToString();
            Debug.LogError(guid);
            ExpectMessage(2, "\"type\":\"event\"");
            ExpectMessage(2, guid);

            SentrySdk.CaptureMessage(guid);
            ExpectMessage(3, "\"type\":\"event\"");
            ExpectMessage(3, guid);

            // On Android we'll grep logcat for this string instead of relying on exit code:
            Debug.Log("SMOKE TEST: PASS");

            // Test passed: Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
            Exit(200);

        }
        catch (Exception ex)
        {
            Debug.Log("SMOKE TEST: FAILED");
            if (exitCode == 0)
            {
                Debug.LogError(ex);
                Exit(-1);
            }
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
