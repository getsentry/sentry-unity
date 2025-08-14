using System;
using Sentry.Unity;
using UnityEngine;
using UnityEngine.UI;
#if SENTRY_HAS_UNITASK
using Cysharp.Threading.Tasks;
#endif
#if SENTRY_HAS_DOTWEEN
using DG.Tweening;
#endif

public class ThirdPartySampleButtons : MonoBehaviour
{
    [SerializeField] private Button _doTweenButton;
    [SerializeField] private Button _uniTaskThrowButton;
    [SerializeField] private Button _uniTaskThrowAndCatchButton;

    private void Awake()
    {
#if !SENTRY_HAS_DOTWEEN
        _doTweenButton.interactable = false;
        _doTweenButton.GetComponentInChildren<Text>().text = "Requires DOTween";
#endif

#if !SENTRY_HAS_UNITASK
        _uniTaskThrowButton.interactable = false;
        _uniTaskThrowButton.GetComponentInChildren<Text>().text = "Requires UniTask";
        _uniTaskThrowAndCatchButton.interactable = false;
        _uniTaskThrowAndCatchButton.GetComponentInChildren<Text>().text = "Requires UniTask";
#endif
    }

    public void ThrowInTween()
    {
#if SENTRY_HAS_DOTWEEN
        var rect = GetComponent<RectTransform>();
        rect.DOPunchScale(new Vector3(1.1f, 1.1f), 0.5f)
            .OnComplete(() => throw new NullReferenceException("There is nothing on complete."));
#endif
    }

    public void ThrowUniTaskException()
    {
#if SENTRY_HAS_UNITASK
        Debug.Log("Starting UniTask that will throw an exception! 🚀");
        DoAsyncWorkAndThrow().Forget();
#endif
    }

    public void ThrowUniTaskExceptionWithCatch()
    {
#if SENTRY_HAS_UNITASK
        Debug.Log("Starting UniTask with proper error handling! 🛡️");
        _ = DoAsyncWorkAndThrowWithCatch();
#endif
    }

#if SENTRY_HAS_UNITASK
    private async UniTask DoAsyncWorkAndThrow()
    {
        Debug.Log("UniTask started - doing some async work... 🔄");

        await UniTask.Delay(200);
        Debug.Log("UniTask work phase 1 complete ✅");

        await UniTask.Delay(200);
        Debug.Log("UniTask work phase 2 complete ✅");

        throw new InvalidOperationException("UniTask async operation failed! 💥🐛");
    }

    private async UniTask DoAsyncWorkAndThrowWithCatch()
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
