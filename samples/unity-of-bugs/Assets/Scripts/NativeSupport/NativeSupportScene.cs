using UnityEngine;

public class NativeSupportScene : MonoBehaviour
{
    [SerializeField] private GameObject _androidButtons;
    [SerializeField] private GameObject _iosButtons;
    [SerializeField] private GameObject _webglButtons;

    private void Start()
    {
#if UNITY_EDITOR || !UNITY_ANDROID
        _androidButtons.SetActive(false);
#endif
#if UNITY_EDITOR || !PLATFORM_IOS
        _iosButtons.SetActive(false);
#endif
        // TODO: webgl native buttons support is currently not available - it requires a javascript error handling
#if true || UNITY_EDITOR || !PLATFORM_WEBGL
        _webglButtons.SetActive(false);
#endif
    }
}
