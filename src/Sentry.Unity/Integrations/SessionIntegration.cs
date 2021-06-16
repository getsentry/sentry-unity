using System;
using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Integrations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Integrations
{
    public class SessionIntegration : ISdkIntegration
    {
        private readonly ISceneManager _sceneManager;
        private readonly ISystemClock _systemClock;

        private const string LastLeaveKey = "LastApplicationLeave";

        public SessionIntegration() : this(SceneManagerAdapter.Instance, SystemClock.Clock)
        {
        }

        internal SessionIntegration(ISceneManager sceneManager, ISystemClock systemClock)
        {
            _sceneManager = sceneManager;
            _systemClock = systemClock;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            if (options is not SentryUnityOptions {EnableAutoSessionTracking: true} unityOptions)
            {
                return;
            }

            options.DiagnosticLogger?.LogDebug("Registering Session integration.");

            _sceneManager.ActiveSceneChanged += CreateFocusListenerGameObject;
            void CreateFocusListenerGameObject(SceneAdapter fromScene, SceneAdapter toScene)
            {
                var gameListenerObject = new GameObject("SentryListener");
                gameListenerObject.hideFlags = HideFlags.HideInHierarchy;

                var gameListener = gameListenerObject.AddComponent<ApplicationFocusListener>();
                gameListener.ApplicationFocusGaining += () =>
                {
                    // No entry in PlayerPrefs means there has been no previous unclosed session.
                    if (!PlayerPrefs.HasKey(LastLeaveKey))
                    {
                        // TODO: check for .NET init session to not override it
                        hub.StartSession();
                        options.DiagnosticLogger?.LogDebug("Starting session.");
                        return;
                    }

                    var dateTimeString = PlayerPrefs.GetString(LastLeaveKey);
                    PlayerPrefs.DeleteKey(LastLeaveKey);

                    if (!DateTimeOffset.TryParse(dateTimeString, out var lastApplicationLeave))
                    {
                        return;
                    }

                    var inactiveDuration = _systemClock.GetUtcNow() - lastApplicationLeave;
                    if (inactiveDuration.TotalSeconds < unityOptions.SessionFocusTimeout)
                    {
                        return;
                    }

                    // TODO: fetch session end status
                    options.DiagnosticLogger?.LogDebug("Ending session.");
                    hub.EndSession();
                    options.DiagnosticLogger?.LogDebug("Starting new session.");
                    hub.StartSession();
                };

                gameListener.ApplicationFocusLosing += () =>
                {
                    // TODO: write session end status
                    PlayerPrefs.SetString(LastLeaveKey, _systemClock.GetUtcNow().ToString());
                };
            }
        }
    }
}
