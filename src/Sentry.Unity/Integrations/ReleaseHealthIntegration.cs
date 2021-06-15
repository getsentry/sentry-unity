using System;
using Sentry.Integrations;
using UnityEngine;

namespace Sentry.Unity.Integrations
{
    public class ReleaseHealthIntegration : ISdkIntegration
    {
        private readonly ISceneManager _sceneManager;
        private readonly ITime _time;

        public ReleaseHealthIntegration() : this(SceneManagerAdapter.Instance, TimeAdapter.Instance)
        {
        }

        internal ReleaseHealthIntegration(ISceneManager sceneManager, ITime time)
        {
            _sceneManager = sceneManager;
            _time = time;
        }

        public void Register(IHub hub, SentryOptions options)
        {
            options.DiagnosticLogger?.Log(SentryLevel.Debug, "Registering SceneManager integration.");

            _sceneManager.ActiveSceneChanged += CreateEventListener;
        }

        private void CreateEventListener(SceneAdapter fromScene, SceneAdapter toScene)
        {
            var gameListenerObject = new GameObject("SentryListener");
            gameListenerObject.hideFlags = HideFlags.HideInHierarchy;

            var gameListener = gameListenerObject.AddComponent<GameEventListener>();
            gameListener.ApplicationEnter += OnApplicationEnter;
            gameListener.ApplicationExit += OnApplicationExit;
        }

        private void OnApplicationEnter()
        {
            // If a session has been running and not properly closed
            if (PlayerPrefs.HasKey("ExitTime"))
            {
                var dateTimeString = PlayerPrefs.GetString("ExitTime");
                PlayerPrefs.DeleteKey("ExitTime");

                var lastExit = Convert.ToDateTime(dateTimeString);

                var inactiveDuration = _time.Now - lastExit;
                if (inactiveDuration.TotalSeconds >= 5.0f)
                {
                    // TODO: fetch session end status
                    // SentrySdk.EndSession();
                    Debug.Log("Session end");
                    // SentrySdk.StartSession();
                    Debug.Log("New Session start");
                }
            }
            else
            {
                // SentrySdk.StartSession();
                Debug.Log("Session start");
            }
        }

        private void OnApplicationExit()
        {
            // TODO: save the session end status as well
            PlayerPrefs.SetString("ExitTime", _time.Now.ToString());
        }
    }
}
