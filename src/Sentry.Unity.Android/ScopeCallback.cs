// Implements the io.sentry.ScopeCallback interface.

using System;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class ScopeCallback : AndroidJavaProxy
{
    private readonly Action<AndroidJavaObject> _callback;

    public ScopeCallback(Action<AndroidJavaObject> callback) : base("io.sentry.ScopeCallback")
    {
        _callback = callback;
    }

    // Note: defining the method should be enough with the default Invoke(), but in reality it doesn't work:
    // No such proxy method: Sentry.Unity.Android.SentryJava+ScopeCallback.run(UnityEngine.AndroidJavaObject)
    //   public void run(AndroidJavaObject scope) => UnityEngine.Debug.Log("run() invoked");
    // Therefore, we're overriding the Invoke() instead:
    public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
    {
        try
        {
            if (methodName != "run" || args.Length != 1)
            {
                throw new Exception($"Invalid invocation: {methodName}({args.Length} args)");
            }
            _callback(args[0]);
        }
        catch (Exception e)
        {
            // Adding the Sentry logger tag ensures we don't send this error to Sentry.
            Debug.unityLogger.Log(LogType.Error, UnityLogger.LogTag, $"Error in SentryJava.ScopeCallback: {e}");
        }
        return null;
    }
}
