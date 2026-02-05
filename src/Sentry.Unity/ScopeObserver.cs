using Sentry.Extensibility;
using Sentry.Unity.Json;

namespace Sentry.Unity;

/// <summary>
/// Sentry Unity Scope Observer wrapper for the common behaviour across platforms.
/// </summary>
public abstract class ScopeObserver : IScopeObserver
{
    private readonly SentryOptions _options;
    private readonly string _name;

    public ScopeObserver(
        string name, SentryOptions options)
    {
        _name = name;
        _options = options;
    }

    public void AddBreadcrumb(Breadcrumb breadcrumb)
    {
        _options.LogDebug("{0} Scope Sync - Adding breadcrumb m:\"{1}\" l:\"{2}\"", _name,
            breadcrumb.Message, breadcrumb.Level);
        AddBreadcrumbImpl(breadcrumb);
    }

    public abstract void AddBreadcrumbImpl(Breadcrumb breadcrumb);

    public void SetExtra(string key, object? value)
    {
        var serialized = value is null ? null : SafeSerializer.SerializeSafely(value);
        if (value is not null && serialized is null)
        {
            _options.LogWarning("{0} Scope Sync - SetExtra k:\"{1}\" v:\"{2}\" - value was serialized as null",
                _name, key, value);
        }
        else
        {
            _options.LogDebug("{0} Scope Sync - Setting Extra k:\"{1}\" v:\"{2}\"", _name, key, value);
        }
        SetExtraImpl(key, serialized);
    }

    public abstract void SetExtraImpl(string key, string? value);

    public void SetTag(string key, string value)
    {
        _options.LogDebug("{0} Scope Sync - Setting Tag k:\"{1}\" v:\"{2}\"", _name, key, value);
        SetTagImpl(key, value);
    }

    public abstract void SetTagImpl(string key, string value);

    public void UnsetTag(string key)
    {
        _options.LogDebug("{0} Scope Sync - Unsetting Tag k:\"{1}\"", _name, key);
        UnsetTagImpl(key);
    }

    public abstract void UnsetTagImpl(string key);

    public void SetUser(SentryUser? user)
    {
        if (user is null)
        {
            _options.LogDebug("{0} Scope Sync - Unsetting User", _name);
            UnsetUserImpl();
        }
        else
        {
            _options.LogDebug("{0} Scope Sync - Setting User i:\"{1}\" n:\"{2}\"", _name, user.Id,
                user.Username);
            SetUserImpl(user);
        }
    }

    public abstract void SetUserImpl(SentryUser user);

    public abstract void UnsetUserImpl();

    public void SetTrace(SentryId traceId, SpanId spanId)
    {
        _options.LogDebug("{0} Scope Sync - Setting Trace traceId:{1} spanId:{2}", _name, traceId, spanId);
        SetTraceImpl(traceId, spanId);
    }

    public abstract void SetTraceImpl(SentryId traceId, SpanId spanId);
}
