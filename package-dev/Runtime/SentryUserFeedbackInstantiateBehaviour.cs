using UnityEngine;
using UnityEngine.UI;

public class SentryUserFeedbackInstantiateBehaviour : MonoBehaviour
{
    [SerializeField] private GameObject _userFeedbackPrefab;
    [SerializeField] private Button _button;

    private GameObject _instantiatedUserFeedback;

    private void Awake()
    {
        if (_button != null)
        {
            _button.onClick.AddListener(OnClick);
        }
    }

    public void OnClick()
    {
        // If the form is already instantiated, don't create a new one
        if (_instantiatedUserFeedback != null)
        {
            return;
        }

        if (_userFeedbackPrefab == null)
        {
            return;
        }

        _instantiatedUserFeedback = Instantiate(_userFeedbackPrefab);
    }
}
