using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// Runs samples from the menu and keeps the status line's text. It outlives scene loads, so a sample that loads a
    /// scene still finishes.
    /// </summary>
    public class SampleRunner : MonoBehaviour
    {
        private static SampleRunner _instance;
        private static volatile string _reported;

        public static SampleRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject(nameof(SampleRunner));
                    DontDestroyOnLoad(host);
                    _instance = host.AddComponent<SampleRunner>();
                }

                return _instance;
            }
        }

        /// <summary>The status line's text.</summary>
        public static string Status { get; private set; }

        public static event Action<string> StatusChanged;

        public static bool IsRunning { get; private set; }

        // Play mode can start without a domain reload, which keeps the last session's statics.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _reported = null;
            Status = null;
            StatusChanged = null;
            IsRunning = false;
            SampleCatalogue.RunOn = RunOn.MainThread;
        }

        /// <summary>A sample's own outcome, shown in the status line in place of the last event's ID.</summary>
        public static void Report(string outcome) => _reported = outcome;

        public void Run(SampleEntry entry) => StartCoroutine(RunRoutine(entry));

        private IEnumerator RunRoutine(SampleEntry entry)
        {
            if (IsRunning)
            {
                SetStatus("Another sample is still running");
                yield break;
            }

            IsRunning = true;
            _reported = null;
            var lastEventId = SentrySdk.LastEventId;
            SetStatus($"Running: {entry.Title}");
            Debug.Log($"Running sample: {entry.Id}");

            // Stepped here rather than yielded to Unity, so a throw ends the sample and not the runner. Unity reports
            // the throw the same way it does for one out of a button's click handler.
            var routine = entry.Run();
            while (true)
            {
                object current;
                try
                {
                    if (!routine.MoveNext())
                    {
                        break;
                    }

                    current = routine.Current;
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    break;
                }

                yield return current;
            }

            IsRunning = false;

            var outcome = _reported;
            if (outcome == null)
            {
                var eventId = SentrySdk.LastEventId;
                outcome = eventId != lastEventId && eventId != SentryId.Empty
                    ? $"Last event: {eventId}"
                    : $"Done: {entry.Title}";
            }

            SetStatus(outcome);
        }

        private static void SetStatus(string status)
        {
            Status = status;
            StatusChanged?.Invoke(status);
        }
    }
}
