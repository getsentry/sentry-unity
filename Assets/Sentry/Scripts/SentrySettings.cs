using System;
using UnityEngine;

namespace Sentry
{
    [CreateAssetMenu(fileName = "SentrySettings", menuName = "Sentry/Sentry Settings", order = 1)]
    [Serializable]
    public class SentrySettings : ScriptableObject
    {
        [SerializeField] [Header("DSN of your sentry instance")]
        public string Dsn;

        [SerializeField] [Header("Send PII like User and Computer names")]
        public bool SendDefaultPii = true;

        [Header("Enable SDK debug messages")]
        public bool Debug = true;

        [Header("Override game version")]
        public string Version = "";

        private static SentrySettings _instance;

        public static SentrySettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    SentrySettings settings = Resources.Load<SentrySettings>(nameof(SentrySettings)) ?? Create();
                    _instance = Instantiate(settings);
                }

                return _instance;
            }
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(Dsn))
            {
                throw new SentryException("No DSN defined. The Sentry SDK will be disabled.");
            }
        }
        
        private static SentrySettings Create()
        {
            return CreateInstance<SentrySettings>();
        }
    }
}