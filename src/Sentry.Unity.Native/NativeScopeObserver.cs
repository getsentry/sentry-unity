using Sentry.Extensibility;
using Sentry.Unity.Json;
using UnityEngine;

namespace Sentry.Unity.Native
{
    /// <summary>
    /// Scope Observer for Native through P/Invoke.
    /// </summary>
    /// <see href="https://github.com/getsentry/sentry-native"/>
    public class NativeScopeObserver : IScopeObserver
    {
        private readonly SentryOptions _options;

        public NativeScopeObserver(SentryOptions options) => _options = options;

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Adding breadcrumb m:\"{0}\" l:\"{1}\"",
                breadcrumb.Message,
                breadcrumb.Level);
            SentryNativeBridge.AddBreadcrumb(breadcrumb);
        }

        public void SetExtra(string key, object? value)
        {
            _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Setting Extra k:\"{0}\" v:\"{1}\"", key, value);
            // TODO implement
        }

        public void SetTag(string key, string value)
        {
            _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Setting Tag k:\"{0}\" v:\"{1}\"", key, value);
            // TODO implement
        }

        public void UnsetTag(string key)
        {
            _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Unsetting Tag k:\"{0}\"", key);
            // TODO implement
        }

        public void SetUser(User? user)
        {
            if (user is not null)
            {
                _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Setting User i:\"{0}\" n:\"{1}\"",
                    user.Id,
                    user.Username);
                // TODO implement
            }
            else
            {
                _options.DiagnosticLogger?.LogDebug("Native Scope Sync - Unsetting User");
                // TODO implement
            }
        }
    }
}
