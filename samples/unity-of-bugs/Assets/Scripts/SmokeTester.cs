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
        var t = new TestHandler();
        try
        {
            Debug.Log("SMOKE TEST: Start");

            var options = new SentryUnityOptions();
            options.Dsn = "https://key@sentry/project";
            options.Debug = true;
            // TODO: Must be set explicitly for the time being.
            options.RequestBodyCompressionLevel = CompressionLevelWithAuto.Auto;
            options.DiagnosticLogger = new ConsoleDiagnosticLogger(SentryLevel.Debug);
            options.CreateHttpClientHandler = () => t;

            var sentryUnityInfo = new SentryUnityInfo();

#if SENTRY_NATIVE_IOS
            Debug.Log("SMOKE TEST: Configure Native iOS.");
            SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
            Debug.Log("SMOKE TEST: Configure Native Android.");
            SentryNativeAndroid.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE_WINDOWS
            Debug.Log("SMOKE TEST: Configure Native Windows.");
            SentryNative.Configure(options);
#endif

            Debug.Log("SMOKE TEST: SentryUnity Init.");
            SentryUnity.Init(options);

            Debug.Log("SMOKE TEST: SentryUnity Init OK.");

            var currentMessage = 0;
            t.ExpectMessage(currentMessage, "'type':'session'");
            t.ExpectMessage(currentMessage, "'init':");

            // if first message was init:false, wait for another one with init:true (this happens on windows...)
            if (t.GetMessage(currentMessage).Contains("\"init\":false"))
            {
                t.ExpectMessage(++currentMessage, "'type':'session'");
                t.ExpectMessage(currentMessage, "'init':true");
            }

            var guid = Guid.NewGuid().ToString();
            Debug.LogError(guid);
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, guid);

            SentrySdk.CaptureMessage(guid);
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, guid);

            var ex = new Exception("Exception & context test");
            SentrySdk.AddBreadcrumb("crumb", "bread", "error", new Dictionary<string, string>() { { "foo", "bar" } }, BreadcrumbLevel.Critical);
            SentrySdk.ConfigureScope((Scope scope) =>
            {
                scope.SetExtra("extra-key", 42);
                scope.AddBreadcrumb("scope-crumb");
                scope.SetTag("tag-key", "tag-value");
                scope.User = new User()
                {
                    Username = "username",
                    Email = "email@example.com",
                    IpAddress = "::1",
                    Id = "user-id",
                    Other = new Dictionary<string, string>() { { "role", "admin" } }
                };
            });
            SentrySdk.CaptureException(ex);
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, "'message':'crumb','type':'error','data':{'foo':'bar'},'category':'bread','level':'critical'}");
            t.ExpectMessage(currentMessage, "'message':'scope-crumb'}");
            t.ExpectMessage(currentMessage, "'extra':{'extra-key':42}");
            t.ExpectMessage(currentMessage, "'tags':{'tag-key':'tag-value'");
            t.ExpectMessage(currentMessage, "'user':{'email':'email@example.com','id':'user-id','ip_address':'::1','username':'username','other':{'role':'admin'}}");

            t.Pass();
        }
        catch (Exception ex)
        {
            if (t.exitCode == 0)
            {
                Debug.Log($"SMOKE TEST: FAILED with exception {ex}");
                t.Exit(-1);
            }
            else
            {
                Debug.Log("SMOKE TEST: FAILED");
            }
        }
    }

    private class TestHandler : HttpClientHandler
    {
        private List<string> _requests = new List<string>();

        private AutoResetEvent _requestReceived = new AutoResetEvent(false);

        private readonly TimeSpan _receiveTimeout = TimeSpan.FromSeconds(10);

        private int _testNumber = 0;

        public int exitCode = 0;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Receive(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }

        private void Receive(HttpRequestMessage message)
        {
            var msgText = message.Content.ReadAsStringAsync().Result;
            lock (_requests)
            {
                Debug.Log($"SMOKE TEST: Intercepted HTTP Request #{_requests.Count} = {msgText}");
                _requests.Add(msgText);
                _requestReceived.Set();
            }
        }

        public void Exit(int code)
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

        public void Pass()
        {
            if (exitCode == 0)
            {
                // On Android we'll grep logcat for this string instead of relying on exit code:
                Debug.Log("SMOKE TEST: PASS");

                // Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
                exitCode = 200;
                Application.Quit(exitCode);
            }
        }

        public void Expect(String message, bool result)
        {
            _testNumber++;
            Debug.Log($"SMOKE TEST | {_testNumber}. {message}: {(result ? "PASS" : "FAIL")}");
            if (!result)
            {
                Debug.Log($"SMOKE TEST: Quitting due to a failed test case #{_testNumber} {message}");
                Exit(_testNumber);
            }
        }

        public string GetMessage(int index)
        {
            while (true)
            {
                lock (_requests)
                {
                    if (_requests.Count > index)
                        break;
                }
                if (!_requestReceived.WaitOne(_receiveTimeout))
                {
                    Debug.Log($"SMOKE TEST: Failed while waiting for an HTTP request #{index} to come in.");
                    Exit(_testNumber);
                }
            }
            lock (_requests)
            {
                return _requests[index];
            }
        }

        public void ExpectMessage(int index, String substring)
        {
            var message = GetMessage(index);
            Expect($"HTTP Request #{index} contains \"{substring}\".",
               message.Contains(substring) || message.Contains(substring.Replace("'", "\"")));
        }
    }
}
