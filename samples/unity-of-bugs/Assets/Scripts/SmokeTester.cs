#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_STANDALONE_WIN && ENABLE_IL2CPP
#define SENTRY_NATIVE_WINDOWS
#endif
#endif

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#elif SENTRY_NATIVE_WINDOWS
using Sentry.Unity.Native;
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
using System.Runtime.InteropServices;
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
        string arg = null;
#if SENTRY_NATIVE_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            arg = intent.Call<String> ("getStringExtra", "test");
        }
#elif UNITY_IOS
        string pListPath = Path.Combine(Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')), "Info.plist");
        using (var streamReader = new StreamReader(pListPath, true))
        {
            var rawPlist = streamReader.ReadToEnd();
            var key = "RunSentrySmokeTest";
            if (rawPlist.Contains(key))
            {
                arg = "smoke";
                Debug.Log("Key " + key + " found on Info.plist starting Smoke test.");
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
            arg = args[2];
        }
#endif
        if (arg == null)
        {
            Debug.Log($"SmokeTest not executed - no argument given");
        }
        else if (arg == "smoke")
        {
            SmokeTest();
        }
        else if (arg == "smoke-crash")
        {
            SmokeTestCrash();
        }
        else if (arg == "post-crash")
        {
            PostCrashTest();
        }
        else
        {
            Debug.Log($"Unknown command line argument: {arg}");
            Application.Quit(-1);
        }
    }

    public static void InitSentry(SentryUnityOptions options)
    {
        options.Dsn = "http://publickey@localhost:8000/12345";
        options.Debug = true;
        options.DebugOnlyInEditor = false;
        // Need to set the logger explicitly so it's available already for the native .Configure() methods.
        // SentryUnity.Init() would set it to the same value, but too late for us.
        options.DiagnosticLogger = new UnityLogger(options);

#if SENTRY_NATIVE_IOS
        Debug.Log("SMOKE TEST: Configure Native iOS.");
        options.IosNativeSupportEnabled = true;
        SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
        Debug.Log("SMOKE TEST: Configure Native Android.");
        options.AndroidNativeSupportEnabled = true;
        SentryNativeAndroid.Configure(options, new SentryUnityInfo());
#elif SENTRY_NATIVE_WINDOWS
        Debug.Log("SMOKE TEST: Configure Native Windows.");
        options.WindowsNativeSupportEnabled = true;
        SentryNative.Configure(options);
#else
        Debug.LogWarning("SMOKE TEST: Given platform is not supported for native sentry configuration");
        throw new Exception("Given platform is not supported");
#endif

        Debug.Log("SMOKE TEST: SentryUnity (.net) Init.");
        SentryUnity.Init(options);
        Debug.Log("SMOKE TEST: SentryUnity (.net) Init OK.");
    }

    public static void SmokeTest()
    {
        var t = new TestHandler();
        try
        {
            Debug.Log("SMOKE TEST: Start");

            var options = new SentryUnityOptions() { CreateHttpClientHandler = () => t };
            InitSentry(options);

            t.Expect("options.CrashedLastRun() == false", !options.CrashedLastRun());

            var currentMessage = 0;
            t.ExpectMessage(currentMessage, "'type':'session'");

            var guid = Guid.NewGuid().ToString();
            Debug.LogError(guid);

            // Skip the session init requests (there may be multiple of othem). We can't skip them by a "positive"
            // because they're also repeated with standard events (in an envelope).
            Debug.Log("Skipping all non-event requests");
            for (; currentMessage < 10; currentMessage++)
            {
                if (t.CheckMessage(currentMessage, "'type':'event'"))
                {
                    break;
                }
            }
            Debug.Log($"Done skipping non-event requests. Last one was: #{currentMessage}");

            t.ExpectMessage(currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, guid);

            SentrySdk.CaptureMessage(guid);
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, guid);

            var ex = new Exception("Exception & context test");
            AddContext();
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

    public static void SmokeTestCrash()
    {
        Debug.Log("SMOKE TEST: Start");

        InitSentry(new SentryUnityOptions());

        AddContext();

        Debug.Log("SMOKE TEST: Issuing a native crash (c++ unhandled exception)");
        throw_cpp();

        // shouldn't execute because the previous call should have failed
        Debug.Log("SMOKE TEST: FAIL - unexpected code executed...");
        Application.Quit(-1);
    }

    public static void PostCrashTest()
    {
        var options = new SentryUnityOptions();
        InitSentry(options);

        var t = new TestHandler();
        t.Expect("options.CrashedLastRun() == true", options.CrashedLastRun());
        t.Pass();
    }

    private static void AddContext()
    {
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
    }

    // CppPlugin.cpp
    [DllImport("__Internal")]
    private static extern void throw_cpp();

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

        public bool CheckMessage(int index, String substring)
        {
            var message = GetMessage(index);
            return message.Contains(substring) || message.Contains(substring.Replace("'", "\""));
        }

        public void ExpectMessage(int index, String substring) =>
            Expect($"HTTP Request #{index} contains \"{substring}\".", CheckMessage(index, substring));
    }
}
