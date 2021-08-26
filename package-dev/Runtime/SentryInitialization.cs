using System;
using Sentry.Infrastructure;
using UnityEngine;
using UnityEngine.Scripting;

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
#if UNITY_IOS
                options.ScopeObserver = new UnityNativeScopeObserver(options);
                options.EnableScopeSync = true;
#endif

                SentryUnity.Init(options);

                Debug.Log("<color=red>Configuring the scope.</color>");
                SentrySdk.ConfigureScope(scope =>
                {
                    scope.SetTag("my fancy unity tag", "my value");
                    scope.User = new User
                    {
                        Id = "42",
                        Email = "unity@bridge.awesome"
                    };
                });

                SentrySdk.AddBreadcrumb(null, "Init Breadcrumb", "Unity happiness", "test", null, BreadcrumbLevel.Debug);
            }
        }
    }
}
