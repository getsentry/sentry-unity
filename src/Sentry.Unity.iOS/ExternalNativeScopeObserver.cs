using Sentry.Extensibility;

namespace Sentry.Unity.iOS;

/// <summary>
/// Scope sync for <see cref="NativeInitializationType.External"/>: the app that embeds the game owns the native SDK's
/// user, environment, and trace, so the game doesn't sync them.
/// </summary>
internal sealed class ExternalNativeScopeObserver : NativeScopeObserver
{
    private readonly SentryOptions _options;

    public ExternalNativeScopeObserver(string name, SentryOptions options) : base(name, options) => _options = options;

    public override void SetUserImpl(SentryUser user) => LogNotSynced("user");

    public override void UnsetUserImpl() => LogNotSynced("user");

    public override void SetTraceImpl(SentryId traceId, SpanId spanId) => LogNotSynced("trace");

    public override void SetEnvironmentImpl(string? environment) => LogNotSynced("environment");

    private void LogNotSynced(string property) =>
        _options.DiagnosticLogger?.LogDebug("Not syncing the {0}. The app owns it.", property);
}
