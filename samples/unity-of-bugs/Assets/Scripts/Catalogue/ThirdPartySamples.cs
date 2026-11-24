using System;
using System.Collections;
using UnityEngine;
#if SENTRY_HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif
#if SENTRY_HAS_DOTWEEN
using DG.Tweening;
#endif

namespace Sentry.Unity.Samples
{
    /// <summary>The third-party samples: DOTween and UniTask. They need the packages.</summary>
    public static class ThirdPartySamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Third party",
            new SampleEntry("thirdparty/dotween-exception", "Exception in a DOTween callback",
                "Throws from a tween's OnComplete.",
                ThrowInTween, SampleRequirements.DOTween),
            new SampleEntry("thirdparty/unitask-exception", "Unhandled UniTask exception",
                "A forgotten UniTask that throws after two delays.",
                ThrowUniTaskException, SampleRequirements.UniTask),
            new SampleEntry("thirdparty/unitask-caught-exception", "Caught UniTask exception",
                "Awaits a UniTask that throws, catches it and captures it.",
                ThrowUniTaskExceptionWithCatch, SampleRequirements.UniTask));

        // Each starts work that throws or captures later, then waits for it, so the status line shows its event.
        private static IEnumerator ThrowInTween()
        {
#if SENTRY_HAS_DOTWEEN
            var lastEventId = SentrySdk.LastEventId;
            var completed = false;
            var target = new GameObject("Tweened").transform;
            target.DOPunchScale(new Vector3(1.1f, 1.1f), 0.5f)
                .OnComplete(() =>
                {
                    completed = true;
                    UnityEngine.Object.Destroy(target.gameObject);
                    throw new NullReferenceException("There is nothing on complete.");
                });
            yield return WaitForEvent(lastEventId, () => completed);
#else
            yield break;
#endif
        }

        private static IEnumerator ThrowUniTaskException()
        {
#if SENTRY_HAS_UNITASK
            var lastEventId = SentrySdk.LastEventId;
            var finished = false;
            Debug.Log("Starting UniTask that will throw an exception! 🚀");
            WhenFinished(DoAsyncWorkAndThrow(), () => finished = true).Forget();
            yield return WaitForEvent(lastEventId, () => finished);
#else
            yield break;
#endif
        }

        private static IEnumerator ThrowUniTaskExceptionWithCatch()
        {
#if SENTRY_HAS_UNITASK
            var lastEventId = SentrySdk.LastEventId;
            var finished = false;
            Debug.Log("Starting UniTask with proper error handling! 🛡️");
            WhenFinished(DoAsyncWorkAndThrowWithCatch(), () => finished = true).Forget();
            yield return WaitForEvent(lastEventId, () => finished);
#else
            yield break;
#endif
        }

        // Waits for the work to finish, then a moment for its event. The time limits keep a callback that never
        // comes from holding up the menu.
        private static IEnumerator WaitForEvent(SentryId lastEventId, Func<bool> finished)
        {
            var deadline = Time.realtimeSinceStartup + 5f;
            while (!finished() && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            deadline = Time.realtimeSinceStartup + 1f;
            while (SentrySdk.LastEventId == lastEventId && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }
        }

#if SENTRY_HAS_UNITASK
        // Rethrows what the work throws, so a forgotten task still reaches UniTask's unobserved exception handler.
        private static async UniTask WhenFinished(UniTask work, Action finished)
        {
            try
            {
                await work;
            }
            finally
            {
                finished();
            }
        }

        private static async UniTask DoAsyncWorkAndThrow()
        {
            Debug.Log("UniTask started - doing some async work... 🔄");

            await UniTask.Delay(200);
            Debug.Log("UniTask work phase 1 complete ✅");

            await UniTask.Delay(200);
            Debug.Log("UniTask work phase 2 complete ✅");

            throw new InvalidOperationException("UniTask async operation failed! 💥🐛");
        }

        private static async UniTask DoAsyncWorkAndThrowWithCatch()
        {
            try
            {
                await DoAsyncWorkAndThrow();
            }
            catch (Exception e)
            {
                Debug.Log("Caught UniTask exception - sending to Sentry 📡");
                SentrySdk.CaptureException(e);
            }
        }
#endif
    }
}
