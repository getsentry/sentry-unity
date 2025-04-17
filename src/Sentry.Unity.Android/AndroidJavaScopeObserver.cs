using System;
using UnityEngine;

namespace Sentry.Unity.Android;

/// <summary>
/// Scope Observer for Android through Java (JNI).
/// </summary>
/// <see href="https://github.com/getsentry/sentry-java"/>
internal class AndroidJavaScopeObserver : ScopeObserver
{
    private ISentryJava _sentryJava;

    public AndroidJavaScopeObserver(SentryOptions options, ISentryJava sentryJava) : base("Android", options)
    {
        _sentryJava = sentryJava;
    }

    public override void AddBreadcrumbImpl(Breadcrumb breadcrumb) =>
        _sentryJava.AddBreadcrumb(breadcrumb);

    public override void SetExtraImpl(string key, string? value) =>
        _sentryJava.SetExtra(key, value);

    public override void SetTagImpl(string key, string value) =>
        _sentryJava.SetTag(key, value);

    public override void UnsetTagImpl(string key) =>
        _sentryJava.UnsetTag(key);

    public override void SetUserImpl(SentryUser user) =>
        _sentryJava.SetUser(user);

    public override void UnsetUserImpl() =>
        _sentryJava.UnsetUser();

    public override void SetTraceImpl(SentryId traceId, SpanId spanId) =>
        _sentryJava.SetTrace(traceId, spanId);
}
