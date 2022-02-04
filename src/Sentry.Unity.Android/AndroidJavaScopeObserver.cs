using UnityEngine;

namespace Sentry.Unity.Android
{
    /// <summary>
    /// Scope Observer for Android through Java (JNI).
    /// </summary>
    /// <see href="https://github.com/getsentry/sentry-java"/>
    public class AndroidJavaScopeObserver : IScopeObserver
    {
        private AndroidJavaObject GetSentryJava() => new AndroidJavaClass("io.sentry.Sentry");

        public void AddBreadcrumb(Breadcrumb breadcrumb)
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

        public void SetExtra(string key, string value)
        {
            AndroidJNI.AttachCurrentThread();
            GetSentryJava().CallStatic("setExtra", key, value);
        }

        public void UnsetExtra(string key)
        {
            AndroidJNI.AttachCurrentThread();
            GetSentryJava().CallStatic("setExtra", key, null);
        }

        public void SetTag(string key, string value)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("setTag", key, value);
        }

        public void UnsetTag(string key)
        {
            AndroidJNI.AttachCurrentThread();
            using var sentry = GetSentryJava();
            sentry.CallStatic("removeTag", key);
        }

        public void SetUser(User user)
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
                GetSentryJava().CallStatic("setUser", javaUser);
            }
            finally
            {
                javaUser?.Dispose();
            }
        }

        public void UnsetUser()
        {
            AndroidJNI.AttachCurrentThread();
            GetSentryJava().CallStatic("setUser", null);
        }
    }
}
