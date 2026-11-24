using System;
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

        private static void ThrowInTween()
        {
#if SENTRY_HAS_DOTWEEN
            var target = new GameObject("Tweened").transform;
            target.DOPunchScale(new Vector3(1.1f, 1.1f), 0.5f)
                .OnComplete(() =>
                {
                    UnityEngine.Object.Destroy(target.gameObject);
                    throw new NullReferenceException("There is nothing on complete.");
                });
#endif
        }

        private static void ThrowUniTaskException()
        {
#if SENTRY_HAS_UNITASK
            Debug.Log("Starting UniTask that will throw an exception! 🚀");
            DoAsyncWorkAndThrow().Forget();
#endif
        }

        private static void ThrowUniTaskExceptionWithCatch()
        {
#if SENTRY_HAS_UNITASK
            Debug.Log("Starting UniTask with proper error handling! 🛡️");
            DoAsyncWorkAndThrowWithCatch().Forget();
#endif
        }

#if SENTRY_HAS_UNITASK
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
