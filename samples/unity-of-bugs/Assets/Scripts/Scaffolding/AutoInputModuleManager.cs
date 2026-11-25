using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem.UI;
#elif !ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.EventSystems;
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
#elif ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            gameObject.AddComponent<InputSystemUIInputModule>();
#else
            Debug.LogError("Failed to detect input system. Sample scene might be unresponsive.");
#endif
        }
    }
}
