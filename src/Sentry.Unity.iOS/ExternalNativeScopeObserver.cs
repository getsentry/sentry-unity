namespace Sentry.Unity.iOS;

/// <summary>
/// Scope sync for <see cref="NativeInitializationType.External"/>: the app that embeds the game owns the native SDK's
/// user, environment, and trace, so the game doesn't sync them.
/// </summary>
internal sealed class ExternalNativeScopeObserver : NativeScopeObserver
{
    public ExternalNativeScopeObserver(string name, SentryOptions options) : base(name, options) { }

    public override void SetUserImpl(SentryUser user) { }

    public override void UnsetUserImpl() { }

    public override void SetTraceImpl(SentryId traceId, SpanId spanId) { }

    public override void SetEnvironmentImpl(string? environment) { }
}
