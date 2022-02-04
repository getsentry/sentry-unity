using Sentry.Extensibility;
using Sentry.Unity.Json;

namespace Sentry.Unity
{
    /// <summary>
    /// Sentry Unity Scope Observer wrapper for the common behaviour accross platforms.
    /// </summary>
    public class ScopeObserver : Sentry.IScopeObserver
    {
        private readonly SentryOptions _options;
        private readonly Sentry.Unity.IScopeObserver _delegate;
        private readonly string _name;

        public ScopeObserver(
            string name, SentryOptions options, Sentry.Unity.IScopeObserver delegateObserver)
        {
            _name = name;
            _options = options;
            _delegate = delegateObserver;
        }

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                "{0} Scope Sync - Adding breadcrumb m:\"{1}\" l:\"{2}\"", null, _name,
                breadcrumb.Message, breadcrumb.Level);
            _delegate.AddBreadcrumb(breadcrumb);
        }

        public void SetExtra(string key, object? value)
        {
            string? extraValue = null;
            if (value is not null)
            {
                extraValue = SafeSerializer.SerializeSafely(value);
                if (extraValue is null)
                {
                    // TODO shouldn't we call UnsetExtra() instead?
                    _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                        "{0} Scope Sync - ignoring SetExtra k:\"{1}\" v:\"{2}\" - value was serialized as null",
                        null, _name, key, value);
                    return;
                }
            }

            if (extraValue is null)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "{0} Scope Sync - Unsetting Extra k:\"{1}\"", null, _name, key);
                _delegate.UnsetExtra(key);
            }
            else
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "{0} Scope Sync - Setting Extra k:\"{1}\" v:\"{2}\"", null, _name, key, value);
                _delegate.SetExtra(key, extraValue);
            }
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                "{0} Scope Sync - Setting Tag k:\"{1}\" v:\"{2}\"", null, _name, key, value);
            _delegate.SetTag(key, value);
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.Log(
                SentryLevel.Debug, "{0} Scope Sync - Unsetting Tag k:\"{1}\"", null, _name, key);
            _delegate.UnsetTag(key);
        }

        public void SetUser(User? user)
        {
            if (user is not null)
            {
                _options.DiagnosticLogger?.Log(SentryLevel.Debug,
                    "{0} Scope Sync - Setting User i:\"{1}\" n:\"{2}\"", null, _name, user.Id,
                    user.Username);
                _delegate.SetUser(user);
            }
            else
            {
                _options.DiagnosticLogger?.Log(
                    SentryLevel.Debug, "{0} Scope Sync - Unsetting User", null, _name);
                _delegate.UnsetUser();
            }
        }
    }
}
