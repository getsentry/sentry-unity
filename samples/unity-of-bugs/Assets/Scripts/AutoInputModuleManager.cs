using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Sentry.Unity.Samples
{
    /// <summary>
    /// Automatically adds the supported Input System to the EventSystem, based
    /// on whether the old one, or the new one is active
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class AutoInputModuleManager : MonoBehaviour
    {
        private void Awake()
        {
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
            gameObject.AddComponent<InputSystemUIInputModule>();
#elif !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            gameObject.AddComponent<StandaloneInputModule>();
#else
            gameObject.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
