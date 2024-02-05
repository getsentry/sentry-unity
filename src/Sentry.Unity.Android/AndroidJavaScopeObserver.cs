using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// Scope Observer for Android through Java (JNI).
    /// </summary>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    public class AndroidJavaScopeObserver : ScopeObserver
    {
        public AndroidJavaScopeObserver(SentryOptions options) : base("Android", options) { }

        private AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

        public override void AddBreadcrumbImpl(Breadcrumb breadcrumb)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            using var javaBreadcrumb = new AndroidJavaObject("io.sentry.Breadcrumb");
            javaBreadcrumb.Set("message", breadcrumb.Message);
            javaBreadcrumb.Set("type", breadcrumb.Type);
            javaBreadcrumb.Set("category", breadcrumb.Category);
            using var javaLevel = breadcrumb.Level.ToJavaSentryLevel();
            javaBreadcrumb.Set("level", javaLevel);
            sentry.CallStatic("addBreadcrumb", javaBreadcrumb, null);
        }

        public override void SetExtraImpl(string key, string? value)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("setExtra", key, value);
        }
        public override void SetTagImpl(string key, string value)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        }

        public override void UnsetTagImpl(string key)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        }

        public override void SetUserImpl(SentryUser user)
        {
            AndroidJNI.AttachCurrentThread();

            AndroidJavaObject? javaUser = null;
            try
            {
                javaUser = new AndroidJavaObject("io.sentry.protocol.User");
                javaUser.Set("email", user.Email);
                javaUser.Set("id", user.Id);
                javaUser.Set("username", user.Username);
                javaUser.Set("ipAddress", user.IpAddress);
                using var sentry = GetSentryJava();
                sentry.CallStatic("setUser", javaUser);
            }
            finally
            {
                javaUser?.Dispose();
            }
        }

        public override void UnsetUserImpl()
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("setUser", null);
        }
    }
}
