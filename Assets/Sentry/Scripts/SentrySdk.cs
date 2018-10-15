using System;
#if UNITY_5
using System.Collections;
#endif
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sentry;
using UnityEngine.Networking;
using UnityDebug = UnityEngine.Debug;

public class SentrySdk : MonoBehaviour
{
    private readonly object _errors = new object();
    private float _timeLastError = 0;
    private const float MIN_TIME = 0.5f;
    public const int MAX_BREADCRUMBS = 100;
    private Breadcrumb[] _breadcrumbs;
    private int _lastBreadcrumbPos = 0;
    private int _noBreadcrumbs = 0;

    [Header("DSN of your sentry instance")]
    public string Dsn;
    [Header("Enable SDK debug messages")]
    public bool Debug = true;
    [Header("Override game version")]
    public string Version = "";

    private string _lastErrorMessage = "";
    private Dsn _dsn;
    private bool _initialized = false;

    private static SentrySdk SentrySdkSingleton = null;

    public void Start()
    {
        if (Dsn == string.Empty)
        {
            // Empty string = disabled SDK
            return;
        }

        _dsn = new Dsn(Dsn);
        if (SentrySdkSingleton != null)
        {
            throw new Exception("Cannot have more than one instance of SentrySdk");
        }
        _breadcrumbs = new Breadcrumb[MAX_BREADCRUMBS];
        SentrySdkSingleton = this;
        _initialized = true; // don't initialize if dsn is empty or something exploded
                            // when parsing dsn
    }

    public static void AddBreadcrumb(string message)
    {
        if (SentrySdkSingleton == null)
        {
            return;
        }

        SentrySdkSingleton.DoAddBreadcrumb(message);
    }

    public static void CaptureMessage(string message)
    {
        if (SentrySdkSingleton == null)
        {
            return;
        }

        SentrySdkSingleton.DoCaptureMessage(message);
    }

    private void DoCaptureMessage(string message)
    {
        StartCoroutine(DoSentrySendMessage(message));
    }

    private void DoAddBreadcrumb(string message)
    {
        if (!_initialized)
        {
            throw new Exception("sentry not initialized");
        }
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        _breadcrumbs[_lastBreadcrumbPos] = new Breadcrumb(timestamp, message);
        _lastBreadcrumbPos += 1;
        _lastBreadcrumbPos %= MAX_BREADCRUMBS;
        if (_noBreadcrumbs < MAX_BREADCRUMBS)
        {
            _noBreadcrumbs += 1;
        }
    }

    public void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLogCallback;
    }

    public void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogCallback;
    }

    public void OnGUI()
    {
        if (_lastErrorMessage != "")
        {
            GUILayout.TextArea(_lastErrorMessage);
            if (GUILayout.Button("Clear"))
            {
                _lastErrorMessage = "";
            }
        }
    }

    public void ScheduleException(string condition, string stackTrace)
    {
        var stack = new List<StackTraceSpec>();
        var exc = condition.Split(new char[] { ':' }, 2);
        var excType = exc[0];
        var excValue = exc[1].Substring(1); // strip the space
        var stackList = stackTrace.Split('\n');
        // the format is as follows:
        // Module.Class.Method[.Invoke] (arguments) (at filename:lineno)
        // where :lineno is optional, will be ommitted in builds
        for (var i = 0; i < stackList.Length; i++)
        {
            string functionName;
            string filename;
            int lineNo;

            var item = stackList[i];
            if (item == "")
            {
                continue;
            }
            var closingParen = item.IndexOf(')');

            if (closingParen == -1)
            {
                functionName = item;
                lineNo = -1;
                filename = "";
            }
            else
            {
                try
                {
                    functionName = item.Substring(0, closingParen + 1);
                    if (item.Substring(closingParen + 1, 5) != " (at ")
                    {
                        // we did something wrong, failed the check
                        UnityDebug.Log("failed parsing " + item);
                        functionName = item;
                        lineNo = -1;
                        filename = "";
                    }
                    else
                    {
                        var colon = item.LastIndexOf(':', item.Length - 1, item.Length - closingParen);
                        if (closingParen == item.Length - 1)
                        {
                            filename = "";
                            lineNo = -1;
                        }
                        else if (colon == -1)
                        {
                            filename = item.Substring(closingParen + 6, item.Length - closingParen - 7);
                            lineNo = -1;
                        }
                        else
                        {
                            filename = item.Substring(closingParen + 6, colon - closingParen - 6);
                            lineNo = Convert.ToInt32(item.Substring(colon + 1, item.Length - 2 - colon));
                        }
                    }
                }
                catch (Exception)
                {
                    functionName = item;
                    lineNo = -1;
                    filename = ""; // we have no clue
                }
            }
            stack.Add(new StackTraceSpec(filename, functionName, lineNo));
        }
        StartCoroutine(DoSendException(excType, excValue, stack));
    }

    public void HandleLogCallback(string condition, string stackTrace, LogType type)
    {
        if (!_initialized)
        {
            return; // dsn not initialized or something exploded, don't try to send it
        }
        _lastErrorMessage = condition;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
        {
            // only send errors, can be set somewhere what we send and what we don't
            return;
        }

        lock (_errors)
        {
            if (Time.time - _timeLastError <= MIN_TIME) 
            {
                return; // silently drop the event on the floor
            }
            _timeLastError = Time.time;
            ScheduleException(condition, stackTrace);
        }
    }

    private IEnumerator
#if !UNITY_5
        <UnityWebRequestAsyncOperation>
#endif
         DoSentrySendMessage(string message)
    {
        if (Debug)
        {
            UnityDebug.Log("sending message to sentry...");
        }
        var guid = Guid.NewGuid().ToString("N");
        var bcrumbs = Breadcrumb.CombineBreadcrumbs(_breadcrumbs,
                                                    _lastBreadcrumbPos,
                                                    _noBreadcrumbs);
        var gameVersion = Version;
        if (gameVersion == "") 
        {
            gameVersion = Application.version;
        }
        var s = JsonUtility.ToJson(
            new SentryMessage(Version, guid, message, bcrumbs));

        return ContinueSendingMessage(s);
    }

    private IEnumerator
#if !UNITY_5
        <UnityWebRequestAsyncOperation>
#endif
         DoSendException(string exceptionType, string exceptionValue, List<StackTraceSpec> stackTrace)
    {
        if (Debug)
        {
            UnityDebug.Log("sending exception to sentry...");
        }
        var guid = Guid.NewGuid().ToString("N");
        var bcrumbs = Breadcrumb.CombineBreadcrumbs(_breadcrumbs,
                                                    _lastBreadcrumbPos,
                                                    _noBreadcrumbs);
        var s = JsonUtility.ToJson(
            new SentryExceptionMessage(Version, guid, exceptionType, exceptionValue, bcrumbs, stackTrace));
        return ContinueSendingMessage(s);
    }

    private IEnumerator
#if !UNITY_5
        <UnityWebRequestAsyncOperation>
#endif
         ContinueSendingMessage(string s)
    {
        var sentryKey = _dsn.publicKey;
        var sentrySecret = _dsn.secretKey;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        var authString = string.Format("Sentry sentry_version=5,sentry_client=Unity0.1," +
                 "sentry_timestamp={0}," +
                 "sentry_key={1}," +
                 "sentry_secret={2}",
                 timestamp,
                 sentryKey,
                 sentrySecret);

        var www = new UnityWebRequest(_dsn.callUri.ToString());
        www.method = "POST";
        www.SetRequestHeader("X-Sentry-Auth", authString);
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(s));
        www.downloadHandler = new DownloadHandlerBuffer();
#if UNITY_5
        yield return www.Send();
#else
        yield return www.SendWebRequest();
#endif

        while (!www.isDone)
        {
            yield return null;
        }
        if (
#if UNITY_5
            www.isError
#else
            www.isNetworkError || www.isHttpError
#endif
             || www.responseCode != 200)
        {
            UnityDebug.LogWarning("error sending request to sentry: " + www.error);
        }
        else if (Debug)
        {
            UnityDebug.Log("Sentry sent back: " + www.downloadHandler.text);
        }
    }
}

