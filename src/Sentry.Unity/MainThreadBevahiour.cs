using System.Collections;
using System.Threading;
using UnityEngine;

namespace Sentry.Unity
{
    // Example of a data storage from main thread.
    internal sealed class MainThreadData
    {
        internal int? MainThreadId { get; set; }

        public string? OperatingSystem { get; set; }

        public bool IsMainThread()
            => MainThreadId.HasValue && Thread.CurrentThread.ManagedThreadId == MainThreadId;
    }

    internal sealed class MainThreadBevahiour : MonoBehaviour
    {
        private static MainThreadBevahiour? _instance;

        internal readonly MainThreadData MainThreadData = new();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            // don't destroy when changing scenes
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
            => StartCoroutine(CollectData());

        private IEnumerator CollectData()
        {
            MainThreadData.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            yield return null;
            MainThreadData.OperatingSystem = SystemInfo.operatingSystem;
        }
    }
}
