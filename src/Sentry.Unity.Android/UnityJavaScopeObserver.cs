using UnityEngine;

namespace Sentry.Unity.Android
{
    // Added only via #ifdef so linker will drop it if not building for Android

    /// <summary>
    /// Scope Observer for Java (JNI).
    /// </summary>
    public class UnityJavaScopeObserver : IScopeObserver
    {
        private readonly AndroidJavaClass _sentry = new("io.sentry.Sentry");

        public UnityJavaScopeObserver(SentryOptions options)
        {
        }

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
            if (value is string)
            {
                _sentry.CallStatic("setExtra", key, value);
            }
            else
            {
                // TODO: JSON serialize before sending down?
                _sentry.CallStatic("setExtra", key, value?.ToString());
            }
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
