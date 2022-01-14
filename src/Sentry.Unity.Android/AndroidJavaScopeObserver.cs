using Sentry.Extensibility;
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

        public AndroidJavaScopeObserver(SentryOptions options) => _options = options;

        private AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

        public void AddBreadcrumb(Breadcrumb breadcrumb)
        {
            AndroidJNI.AttachCurrentThread();

            _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Adding breadcrumb m:\"{0}\" l:\"{1}\"",
                breadcrumb.Message,
                breadcrumb.Level);

            using var sentry = GetSentryJava();
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            using var javaLevel = breadcrumb.Level.ToJavaSentryLevel();
            javaBreadcrumb.Set("level", javaLevel);
            sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        }

        public void SetExtra(string key, object? value)
        {
            AndroidJNI.AttachCurrentThread();

            _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Setting Extra k:\"{0}\" v:\"{1}\"", key, value);

            string? extraValue = null;
            if (value is not null)
            {
                extraValue = SafeSerializer.SerializeSafely(value);
                if (extraValue is null)
                {
                    return;
                }
            }

            using var sentry = GetSentryJava();
            sentry.CallStatic("setExtra", key, extraValue);
        }

        public void SetTag(string key, string value)
        {
            AndroidJNI.AttachCurrentThread();

            _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Setting Tag k:\"{0}\" v:\"{1}\"", key, value);

            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        }

        public void UnsetTag(string key)
        {
            AndroidJNI.AttachCurrentThread();

            _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Unsetting Tag k:\"{0}\"", key);

            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        }

        public void SetUser(User? user)
        {
            AndroidJNI.AttachCurrentThread();

            AndroidJavaObject? javaUser = null;
            try
            {
                if (user is not null)
                {
                    _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Setting User i:\"{0}\" n:\"{1}\"",
                        user.Id,
                        user.Username);

                    javaUser = new AndroidJavaObject("io.sentry.protocol.User");
                    javaUser.Set("email", user.Email);
                    javaUser.Set("id", user.Id);
                    javaUser.Set("username", user.Username);
                    javaUser.Set("ipAddress", user.IpAddress);
                }
                else
                {
                    _options.DiagnosticLogger?.LogDebug("Android Scope Sync - Unsetting User");
                }
                using var sentry = GetSentryJava();
                sentry.CallStatic("setUser", javaUser);
            }
            finally
            {
                javaUser?.Dispose();
            }
        }
    }
}
