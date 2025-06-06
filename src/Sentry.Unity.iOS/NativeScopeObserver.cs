using System;

namespace Sentry.Unity.iOS;

public class NativeScopeObserver : ScopeObserver
{
    public NativeScopeObserver(string name, SentryOptions options) : base(name, options) { }

    public override void AddBreadcrumbImpl(Breadcrumb breadcrumb)
    {
        var level = GetBreadcrumbLevel(breadcrumb.Level);
        var timestamp = GetTimestamp(breadcrumb.Timestamp);

        SentryCocoaBridgeProxy.AddBreadcrumb(timestamp, breadcrumb.Message, breadcrumb.Type, breadcrumb.Category, level);
    }

    public override void SetExtraImpl(string key, string? value) =>
        SentryCocoaBridgeProxy.SetExtra(key, value);

    public override void SetTagImpl(string key, string value) => SentryCocoaBridgeProxy.SetTag(key, value);

    public override void UnsetTagImpl(string key) => SentryCocoaBridgeProxy.UnsetTag(key);

    public override void SetUserImpl(SentryUser user) =>
        SentryCocoaBridgeProxy.SetUser(user.Email, user.Id, user.IpAddress, user.Username);

    public override void UnsetUserImpl() => SentryCocoaBridgeProxy.UnsetUser();

    public override void SetTraceImpl(SentryId traceId, SpanId spanId) =>
        SentryCocoaBridgeProxy.SetTrace(traceId.ToString(), spanId.ToString());

    internal static string GetTimestamp(DateTimeOffset timestamp) =>
        // "o": Using ISO 8601 to make sure the timestamp makes it to the bridge correctly.
        // https://docs.microsoft.com/en-gb/dotnet/standard/base-types/standard-date-and-time-format-strings#Roundtrip
        timestamp.ToString("o");

    internal static int GetBreadcrumbLevel(BreadcrumbLevel breadcrumbLevel) =>
        // https://github.com/getsentry/sentry-cocoa/blob/50f955aeb214601dd62b5dae7abdaddc8a1f24d9/Sources/Sentry/Public/SentryDefines.h#L99-L105
        breadcrumbLevel switch
        {
            BreadcrumbLevel.Debug => 1,
            BreadcrumbLevel.Info => 2,
            BreadcrumbLevel.Warning => 3,
            BreadcrumbLevel.Error => 4,
            BreadcrumbLevel.Critical => 5,
            _ => 0
        };
}
