namespace Sentry.Unity.Android;

/// <summary>
/// Scope sync for <see cref="NativeInitializationType.External"/>. The game follows the app's trace. When attached to
/// the app's SDK, the app also owns the user and environment. In its own process, the game's SDK needs them.
/// </summary>
internal sealed class ExternalAndroidJavaScopeObserver : AndroidJavaScopeObserver
{
    private readonly bool _isAttached;

    public ExternalAndroidJavaScopeObserver(SentryOptions options, ISentryJava sentryJava, bool isAttached)
        : base(options, sentryJava) => _isAttached = isAttached;

    public override void SetUserImpl(SentryUser user)
    {
        if (!_isAttached)
        {
            base.SetUserImpl(user);
        }
    }

    public override void UnsetUserImpl()
    {
        if (!_isAttached)
        {
            base.UnsetUserImpl();
        }
    }

    public override void SetTraceImpl(SentryId traceId, SpanId spanId) { }

    public override void SetEnvironmentImpl(string? environment)
    {
        if (!_isAttached)
        {
            base.SetEnvironmentImpl(environment);
        }
    }
}
