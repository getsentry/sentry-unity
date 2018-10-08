using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using Sentry;
using UnityEngine.Networking;

public class SentrySdk : MonoBehaviour
{
    object errors = new object();
    float timeLastError = 0;
    const float MIN_TIME = 0.5f;
    public const int MAX_BREADCRUMBS = 100;
    Breadcrumb[] breadcrumbs;
    int lastBreadcrumbPos = 0;
    int noBreadcrumbs = 0;

    [Header("DSN of your sentry instance")]
    public string dsn;
    [Header("Set true to get log messages")]
    public bool isNoisy = true;
    [Header("Override game version")]
    public string version = "";

    string lastErrorMessage = "";
    Dsn _dsn;
    bool initialized = false;

    static SentrySdk sentrySdkSingleton = null;

    public void Start()
    {
        _dsn = new Dsn(dsn);
        if (sentrySdkSingleton != null)
        {
            throw new Exception("Cannot have more than one instance of SentrySdk");
        }
        breadcrumbs = new Breadcrumb[MAX_BREADCRUMBS];
        sentrySdkSingleton = this;
        initialized = true; // don't initialize if dsn is empty or something exploded
                            // when parsing dsn
    }

    public static void addBreadcrumb(string message)
    {
        sentrySdkSingleton._addBreadcrumb(message);
    }

    public static void CaptureMessage(string message)
    {
        sentrySdkSingleton._captureMessage(message);
    }

    void _captureMessage(string message)
    {
        if (!initialized)
            throw new Exception("sentry not initialized");    
        StartCoroutine(sentrySendMessage(message));
    }

    void _addBreadcrumb(string message)
    {
        if (!initialized)
            throw new Exception("sentry not initialized");
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        breadcrumbs[lastBreadcrumbPos] = new Breadcrumb(timestamp, message);
        lastBreadcrumbPos += 1;
        lastBreadcrumbPos %= MAX_BREADCRUMBS;
        if (noBreadcrumbs < MAX_BREADCRUMBS)
            noBreadcrumbs += 1;
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
        if (lastErrorMessage != "")
        {
            GUILayout.TextArea(lastErrorMessage);
            if (GUILayout.Button("Clear"))
            {
                lastErrorMessage = "";
            }
        }
    }

    public void scheduleException(string condition, string stackTrace)
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
            string functionName, filename;
            int lineNo;

            var item = stackList[i];
            if (item == "")
                continue;
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
                        Debug.Log("failed parsing " + item);
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
                } catch (Exception) {
                    functionName = item;
                    lineNo = -1;
                    filename = ""; // we have no clue
                }
            }
            stack.Add(new StackTraceSpec(filename, functionName, lineNo));
        }
        StartCoroutine(sendException(excType, excValue, stack));
    }

    public void HandleLogCallback(string condition, string stackTrace, LogType type)
    {
        if (!initialized)
            return; // dsn not initialized or something exploded, don't try to send it
        lastErrorMessage = condition;
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            // only send errors, can be set somewhere what we send and what we don't
            return;
        
        lock (errors)
        {
            if (Time.time - timeLastError <= MIN_TIME) {
                return; // silently drop the event on the floor
            }
            timeLastError = Time.time;
            scheduleException(condition, stackTrace);
        }
    }

    IEnumerator sentrySendMessage(string message)
    {
        if (isNoisy)
            Debug.Log("sending message to sentry...");
        var guid = Guid.NewGuid().ToString("N");
        var bcrumbs = Breadcrumb.CombineBreadcrumbs(breadcrumbs,
                                                    lastBreadcrumbPos,
                                                    noBreadcrumbs);
        var gameVersion = version;
        if (gameVersion == "") {
            gameVersion = Application.version;
        }
        var s = JsonUtility.ToJson(
            new SentryMessage(version, guid, message, bcrumbs));

        return _continueSendingMessage(s);
    }

    IEnumerator sendException(string exceptionType, string exceptionValue, List<StackTraceSpec> stackTrace)
    {
        if (isNoisy)
            Debug.Log("sending exception to sentry...");
        var guid = Guid.NewGuid().ToString("N");
        var bcrumbs = Breadcrumb.CombineBreadcrumbs(breadcrumbs,
                                                    lastBreadcrumbPos,
                                                    noBreadcrumbs);
        var s = JsonUtility.ToJson(
            new SentryExceptionMessage(version, guid, exceptionType, exceptionValue, bcrumbs, stackTrace));
        return _continueSendingMessage(s);
    }

    IEnumerator _continueSendingMessage(string s)
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

        UnityWebRequest www = new UnityWebRequest(_dsn.callUri.ToString());
        www.method = "POST";
        www.SetRequestHeader("X-Sentry-Auth", authString);
        www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(s));
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.Send();

        while (!www.isDone)
            yield return null;
        if (www.isError || www.responseCode != 200)
            Debug.LogWarning("error sending request to sentry: " + www.error);
        else if (isNoisy) {
            Debug.Log("Sentry sent back: " + www.downloadHandler.text);
        }
    }
}

