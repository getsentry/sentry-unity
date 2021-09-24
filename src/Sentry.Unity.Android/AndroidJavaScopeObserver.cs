using Sentry.Unity.Json;
using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// Scope Observer for Android through Java (JNI).
    /// </summary>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    public class AndroidJavaScopeObserver : IScopeObserver
    {
        private readonly SentryOptions _options;
        private readonly AndroidJavaClass _sentry = new("io.sentry.Sentry");

        public AndroidJavaScopeObserver(SentryOptions options) => _options = options;

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            _sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        }

        public void SetExtra(string key, object? value)
        {
            string? extraValue = null;
            if (value is not null)
            {
                extraValue = SafeSerializer.SerializeSafely(value);
                if (extraValue is null)
                {
                    return;
                }
            }
            _sentry.CallStatic("setExtra", key, extraValue);
        }

        public void SetTag(string key, string value)
            => _sentry.CallStatic("setTag", key, value);

        public void UnsetTag(string key)
            => _sentry.CallStatic("removeTag", key);

        public void SetUser(User? user)
        {
            AndroidJavaObject? javaUser = null;
            try
            {
                if (user is not null)
                {
                    javaUser = new AndroidJavaObject("io.sentry.protocol.User");
                    javaUser.Set("email", user.Email);
                    javaUser.Set("id", user.Id);
                    javaUser.Set("username", user.Username);
                    javaUser.Set("ipAddress", user.IpAddress);
                }
                _sentry.CallStatic("setUser", javaUser);
            }
            finally
            {
                javaUser?.Dispose();
            }
        }
    }
}
