#if !UNITY_EDITOR
#if UNITY_IOS
#define SENTRY_NATIVE_IOS
#elif UNITY_ANDROID
#define SENTRY_NATIVE_ANDROID
#elif UNITY_WEBGL
#define SENTRY_WEBGL
#endif
#endif

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

#if SENTRY_NATIVE_IOS
using Sentry.Unity.iOS;
#endif
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Protocol.Envelopes;

[assembly: AlwaysLinkAssembly]

namespace Sentry.Unity
{
    public static class SentryInitialization
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            var options = ScriptableSentryUnityOptions.LoadSentryUnityOptions();
            if (options.ShouldInitializeSdk())
            {

#if SENTRY_NATIVE_IOS
                options.ScopeObserver = new IosNativeScopeObserver(options);
                options.EnableScopeSync = true;
#elif SENTRY_NATIVE_ANDROID
                options.ScopeObserver = new UnityJavaScopeObserver(options);
                options.EnableScopeSync = true;
#elif SENTRY_WEBGL
                // Caching transport relies on a background thread
                options.CacheDirectoryPath = null;
                options.BackgroundWorker = new WebBackgroundWorker(options);

                // Still cant' find out what's using Threads so:
                options.AutoSessionTracking = false;
                options.DetectStartupTime = StartupTimeDetectionMode.None;
                options.DisableTaskUnobservedTaskExceptionCapture();
                options.DisableAppDomainUnhandledExceptionCapture();
                options.DisableAppDomainProcessExitFlush();
                options.DisableDuplicateEventDetection();
                options.ReportAssembliesMode = ReportAssembliesMode.None;
#endif

                SentryUnity.Init(options);
            }
        }
    }

    internal class WebBackgroundWorker : IBackgroundWorker
    {
        private readonly SentryUnityOptions _options;
        private readonly ITransport _transport;

        public WebBackgroundWorker(SentryUnityOptions options)
        {
            _options = options;
            var composer = new SdkComposer(options);
            _transport = composer.CreateTransport();
        }

        public bool EnqueueEnvelope(Envelope envelope)
        {
            _transport.SendEnvelopeAsync(envelope, CancellationToken.None)
                .ContinueWith(r => _options.DiagnosticLogger?.LogInfo("Result of envelope capture was: {0}", r.Status));
            return true;
        }

        public Task FlushAsync(TimeSpan timeout)
        {
            return Task.CompletedTask;
        }

        public int QueuedItems { get; }
    }
}
