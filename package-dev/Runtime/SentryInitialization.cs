#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_STANDALONE_WIN && ENABLE_IL2CPP
#define SENTRY_NATIVE_WINDOWS
#endif
#endif

using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#elif UNITY_ANDROID
using Sentry.Unity.Android;
#elif SENTRY_NATIVE_WINDOWS
using Sentry.Unity.Native;
using Sentry.Extensibility;
using System;
using System.IO;
#endif

[assembly: AlwaysLinkAssembly]

namespace Sentry.Unity
{
    public static class SentryInitialization
    {
#if SENTRY_NATIVE_WINDOWS
        private static FileStream _lockFile;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options.ShouldInitializeSdk())
            {
                var sentryUnityInfo = new SentryUnityInfo();

#if SENTRY_NATIVE_IOS
                SentryNativeIos.Configure(options);
#elif SENTRY_NATIVE_ANDROID
                SentryNativeAndroid.Configure(options, sentryUnityInfo);
#elif SENTRY_NATIVE_WINDOWS
                SentryNative.Configure(options);

                // On Standalone, we disable cache dir in case multiple app instances run over the same path.
                // Note: we cannot use a named Mutex, because Unit doesn't support it. Instead, we create a file with `FileShare.None`.
                // https://forum.unity.com/threads/unsupported-internal-call-for-il2cpp-mutex-createmutex_internal-named-mutexes-are-not-supported.387334/
                if (options.CacheDirectoryPath != null)
                {
                    try
                    {
                        _lockFile = new FileStream(Path.Combine(options.CacheDirectoryPath, "sentry-unity.lock"), FileMode.OpenOrCreate,
                                FileAccess.ReadWrite, FileShare.None);

                        Application.quitting += () =>
                        {
                            try
                            {
                                // We don't really need to close, Windows would do that anyway, but let's be nice.
                                _lockFile.Close();
                            }
                            catch (Exception ex)
                            {
                                options.DiagnosticLogger?.Log(SentryLevel.Warning,
                                    "Exception while releasing the lockfile on the config directory.", ex);
                            }
                        };
                    }
                    catch (Exception ex)
                    {
                        options.DiagnosticLogger?.Log(SentryLevel.Warning, "An exception was thrown while trying to " +
                            "acquire a lockfile on the config directory: .NET event cache will be disabled.", ex);
                        options.CacheDirectoryPath = null;
                        options.AutoSessionTracking = false;
                    }
                }
#endif

                SentryUnity.Init(options);
            }
        }
    }

    public class SentryUnityInfo : ISentryUnityInfo
    {
        public bool IL2CPP
        {
            get =>
#if ENABLE_IL2CPP
               true;
#else
               false;
#endif
        }
    }
}
