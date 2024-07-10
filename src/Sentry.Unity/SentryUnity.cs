using System;
using System.ComponentModel;
using Sentry.Extensibility;

namespace Sentry.Unity;

/// <summary>
/// Sentry Unity initialization class.
/// </summary>
public static class SentryUnity
{
    private static SentryUnitySdk? UnitySdk;

    /// <summary>
    /// Initializes Sentry Unity SDK while configuring the options.
    /// </summary>
    /// <param name="sentryUnityOptionsConfigure">Callback to configure the options.</param>
    public static void Init(Action<SentryUnityOptions> sentryUnityOptionsConfigure)
    {
        var options = new SentryUnityOptions();
        sentryUnityOptionsConfigure.Invoke(options);

        Init(options);
    }

    /// <summary>
    /// Initializes Sentry Unity SDK while providing an options object.
    /// </summary>
    /// <param name="options">The options object.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Init(SentryUnityOptions options)
    {
        if (UnitySdk is not null)
        {
            options.DiagnosticLogger?.LogWarning("The SDK has already been initialized.");
        }

        UnitySdk = SentryUnitySdk.Init(options);
    }

    /// <summary>
    /// Closes the Sentry Unity SDK
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static void Close()
    {
        UnitySdk?.Close();
        UnitySdk = null;
    }
}