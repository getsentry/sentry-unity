using UnityEngine;

namespace Sentry
{
    public class SentryBehavior : MonoBehaviour
    {
        private static SentryBehavior _instance;

        [Header("DSN of your sentry instance")]
        public string dsn = string.Empty;

        private SentryCrashReporter _reporter;

        public void Start()
        {
            if (dsn == string.Empty)
            {
                Debug.Log("No DSN set for SentryBehavior");
                return;
            }

            if (_instance == null)
            {
                DontDestroyOnLoad(this);
                _instance = this;
                _reporter = new SentryCrashReporter(dsn);
                _reporter.Enable();
            }
            else
            {
                Destroy(this);
            }
        }

        public SentryCrashReporter GetReporter()
        {
            return _reporter;
        }

        private void OnEnable()
        {
            if (_reporter != null)
            {
                _reporter.Enable();
            }
        }

        private void OnDisable()
        {
            if (_reporter != null)
            {
                _reporter.Disable();
            }
        }
    }
}