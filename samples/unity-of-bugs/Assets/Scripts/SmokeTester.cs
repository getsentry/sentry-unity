using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Sentry;
using Sentry.Infrastructure;
using Sentry.Unity;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SmokeTester : MonoBehaviour
{
    public void Start()
    {
        var arg = GetTestArg();
        if (arg == "smoke")
        {
            SmokeTest();
        }
        else if (arg == "hasnt-crashed")
        {
            HasntCrashedTest();
        }
        else if (arg == "crash")
        {
            CrashTest();
        }
        else if (arg == "has-crashed")
        {
            HasCrashedTest();
        }
        else if (arg != null)
        {
            Debug.Log($"Unknown command line argument: {arg}");
            Application.Quit(-1);
        }
    }

#if UNITY_IOS && !UNITY_EDITOR
    // .NET `Environment.GetCommandLineArgs()` doesn't seem to work on iOS so we get the test arg in Objective-C
    [DllImport("__Internal", EntryPoint="getTestArgObjectiveC")]
    private static extern string GetTestArg();
#else
    private static string GetTestArg()
    {
        string arg = null;
#if UNITY_EDITOR
#elif UNITY_ANDROID
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (var intent = currentActivity.Call<AndroidJavaObject>("getIntent"))
        {
            arg = intent.Call<String>("getStringExtra", "test");
        }
#elif UNITY_WEBGL
        var uri = new Uri(Application.absoluteURL);
        arg = HttpUtility.ParseQueryString(uri.Query).Get("test");
#else
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 2 && args[1] == "--test")
        {
            arg = args[2];
        }
#endif
        return arg;
    }
#endif

    private static TestHandler t = new TestHandler();

    private static Func<int> _crashedLastRun = () => -1;

    // Forwarded from SmokeTestOptions.Configure()
    public static void Configure(SentryUnityOptions options)
    {
        if (GetTestArg() == null)
        {
            Debug.Log("SmokeTester.Configure() called but skipped because this is not a SmokeTest (no arg)");
            return;
        }

        Debug.Log("SmokeTester.Configure() running");
        options.CreateHttpClientHandler = () => t;
        _crashedLastRun = () =>
        {
            if (options.CrashedLastRun != null)
            {
                return options.CrashedLastRun() ? 1 : 0;
            }
            return -2;
        };
    }

    public static void SmokeTest()
    {
        t.name = "SMOKE";
        try
        {
            Debug.Log("SMOKE TEST: Start");
            int crashed = _crashedLastRun();
            t.Expect($"options.CrashedLastRun ({crashed}) == false (0)", crashed == 0);

            var currentMessage = 0;
            t.ExpectMessage(currentMessage, "'type':'session'");

            var guid = Guid.NewGuid().ToString();
            Debug.LogError($"LogError(GUID)={guid}");

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
            t.ExpectMessage(currentMessage, $"LogError(GUID)={guid}");
            t.ExpectMessage(currentMessage, "'filename':'screenshot.jpg','attachment_type':'event.attachment'");
            t.ExpectMessageNot(currentMessage, "'length':0");

            SentrySdk.CaptureMessage($"CaptureMessage(GUID)={guid}");
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, $"CaptureMessage(GUID)={guid}");
            t.ExpectMessage(currentMessage, "'filename':'screenshot.jpg','attachment_type':'event.attachment'");
            t.ExpectMessageNot(currentMessage, "'length':0");

            var ex = new Exception("Exception & context test");
            AddContext();
            SentrySdk.CaptureException(ex);
            t.ExpectMessage(++currentMessage, "'type':'event'");
            t.ExpectMessage(currentMessage, "'message':'crumb','type':'error','data':{'foo':'bar'},'category':'bread','level':'critical'}");
            t.ExpectMessage(currentMessage, "'message':'scope-crumb'}");
            t.ExpectMessage(currentMessage, "'extra':{'extra-key':42}");
            t.ExpectMessage(currentMessage, "'tags':{'tag-key':'tag-value'");
            t.ExpectMessage(currentMessage, "'user':{'email':'email@example.com','id':'user-id','ip_address':'::1','username':'username','other':{'role':'admin'}}");
            t.ExpectMessage(currentMessage, "'filename':'screenshot.jpg','attachment_type':'event.attachment'");
            t.ExpectMessageNot(currentMessage, "'length':0");

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

    public static void CrashTest()
    {
        Debug.Log("CRASH TEST: Start");

        AddContext();

        Debug.Log("CRASH TEST: Issuing a native crash (c++ unhandled exception)");
        throw_cpp();

        // shouldn't execute because the previous call should have failed
        Debug.Log("CRASH TEST: FAIL - unexpected code executed...");
        Application.Quit(-1);
    }

    public static void HasntCrashedTest()
    {
        t.name = "HASNT-CRASHED";
        int crashed = _crashedLastRun();
        t.Expect($"options.CrashedLastRun ({crashed}) == false (0)", crashed == 0);
        t.Pass();
    }

    public static void HasCrashedTest()
    {
        t.name = "HAS-CRASHED";
        int crashed = _crashedLastRun();
        t.Expect($"options.CrashedLastRun ({crashed}) == true (1)", crashed == 1);
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
        public String name;

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
                Debug.Log($"{name} TEST: Intercepted HTTP Request #{_requests.Count} = {msgText}");
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
                Debug.Log($"{name} TEST: PASS");

                // Exit Code 200 to avoid false positive from a graceful exit unrelated to this test run
                exitCode = 200;

#if !UNITY_WEBGL  // We don't quit on WebGL because outgoing HTTP requests (in coroutines) would be cancelled.
                Application.Quit(exitCode);
#endif
            }
        }

        public void Expect(String message, bool result)
        {
            _testNumber++;
            Debug.Log($"{name} TEST | {_testNumber}. {message}: {(result ? "PASS" : "FAIL")}");
            if (!result)
            {
                Debug.Log($"{name} TEST: FAIL - quitting due to a failed test case #{_testNumber} {message}");
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
                    Debug.Log($"{name} TEST: Failed while waiting for an HTTP request #{index} to come in.");
                    Exit(_testNumber);
                }
            }
            lock (_requests)
            {
                return _requests[index];
            }
        }

        public bool CheckMessage(int index, String substring, bool negate = false)
        {
#if UNITY_WEBGL
            // Note: we cannot use the standard checks on WebGL - it would get stuck here because of the lack of multi-threading.
            // The verification is done in the python script used for WebGL smoke test - smoke-test-webgl.py
            return true;
#else
            var message = GetMessage(index);
            var contains = message.Contains(substring) || message.Contains(substring.Replace("'", "\""));
            return negate ? !contains : contains;
#endif
        }

        public void ExpectMessage(int index, String substring) =>
            Expect($"HTTP Request #{index} contains \"{substring}\".", CheckMessage(index, substring));

        public void ExpectMessageNot(int index, String substring) =>
            Expect($"HTTP Request #{index} doesn't contain \"{substring}\".", CheckMessage(index, substring, negate: true));
    }
}
