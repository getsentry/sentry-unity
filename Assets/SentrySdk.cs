using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Sentry;

public class SentrySdk : MonoBehaviour
{
    [Header("DSN of your sentry instance")]
    public string dsn;
    Dsn _dsn;

    public void Start()
    {
        _dsn = new Dsn(dsn);
    }

    public void OnEnable()
    {
        Application.logMessageReceived += HandleLogCallback;
    }

    public void OnDisable()
    {
        Application.logMessageReceived -= HandleLogCallback;
    }

    public void HandleLogCallback(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            // only send errors, can be set somewhere what we send and what we don't
            return;
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
            var firstSpace = item.IndexOf(' ');

            if (firstSpace == -1)
            {
                functionName = item;
                lineNo = -1;
                filename = "";
            }
            else
            {
                functionName = item.Substring(0, firstSpace);
                // we can try to split functionName into module.function, but it's not 100% clear how
                var closingParen = item.IndexOf(')', firstSpace);
                if (closingParen == item.Length - 1)
                {
                    // case of some continuations where there is no space between
                    // the () and the method name
                    closingParen = firstSpace - 1;
                }
                var colon = item.IndexOf(':', closingParen);
                if (colon == -1)
                {
                    Debug.Log(item);
                    filename = item.Substring(closingParen + 6, item.Length - closingParen - 7);
                    lineNo = -1;
                }
                else
                {
                    filename = item.Substring(closingParen + 6, colon - closingParen - 6);
                    lineNo = Convert.ToInt32(item.Substring(colon + 1, item.Length - 2 - colon));
                }
            }
            stack.Add(new StackTraceSpec(filename, functionName, lineNo));
        }
        StartCoroutine(sendException(excType, excValue, stack));
    }

    private long ConvertToTimestamp(DateTime value)
    {
        long epoch = (value.Ticks - 621355968000000000) / 10000000;
        return epoch;
    }

    IEnumerator<WWW> sendException(string exceptionType, string exceptionValue, List<StackTraceSpec> stackTrace)
    {
        var guid = Guid.NewGuid().ToString("N");
        var s = JsonUtility.ToJson(
            new SentryExceptionMessage(guid, exceptionType, exceptionValue, stackTrace));
        var sentryKey = _dsn.publicKey;
        var sentrySecret = _dsn.secretKey;

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH\\:mm\\:ss");
        var headers = new Dictionary<string, string>();
        headers["X-Sentry-Auth"] = ($"Sentry sentry_version=5,sentry_client=Unity0.1," +
                 $"sentry_timestamp={timestamp}," +
                 $"sentry_key={sentryKey},sentry_secret={sentrySecret}");

        using (WWW www = new WWW(_dsn.callUri.ToString(), Encoding.UTF8.GetBytes(s), headers))
        {
            yield return www;
            Debug.Log(Encoding.UTF8.GetString(www.bytes));
        }
    }
}

