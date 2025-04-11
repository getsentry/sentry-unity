using System;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity.Android;

internal class AndroidOptionsConfiguration : AndroidJavaProxy
{
    private readonly Action<AndroidJavaObject> _callback;
    private readonly IDiagnosticLogger? _logger;

    public AndroidOptionsConfiguration(Action<AndroidJavaObject> callback, IDiagnosticLogger? logger)
        : base("io.sentry.Sentry$OptionsConfiguration")
    {
        _callback = callback;
        _logger = logger;
    }

    public override AndroidJavaObject? Invoke(string methodName, AndroidJavaObject[] args)
    {
        try
        {
            if (methodName != "configure" || args.Length != 1)
            {
                throw new Exception($"Invalid invocation: {methodName}({args.Length} args)");
            }

            _callback(args[0]);
        }
        catch (Exception e)
        {
            _logger?.LogError(e, "Error invoking {0} ’.", methodName);
        }
        return null;
    }
}
