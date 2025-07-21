using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sentry.Unity
{
    public class SentryUserFeedback : MonoBehaviour
    {
        // Form
        [SerializeField] private GameObject _feedbackForm;

        // Button
        [SerializeField] private GameObject _openFeedbackButton;
        [SerializeField] private Button _sendFeedbackButton;

        // Inputs
        [SerializeField] private InputField _name;
        [SerializeField] private InputField _email;
        [SerializeField] private InputField _description;
        [SerializeField] private Toggle _addScreenshot;

        // Disabling the SentryUserFeedback if the minimum required components are missing
        private void Awake()
        {
            if (!_feedbackForm)
            {
                Debug.LogError("Feedback Form is missing", this);
                gameObject.SetActive(false);
            }

            if (!_openFeedbackButton)
            {
                Debug.LogError("Open Feedback Button is missing", this);
                gameObject.SetActive(false);
            }

            if (!_sendFeedbackButton)
            {
                Debug.LogError("Send Feedback Button is missing", this);
                gameObject.SetActive(false);
            }

            if (!_description)
            {
                Debug.LogError("Description input field is missing", this);
                gameObject.SetActive(false);
            }
        }

        // As long as there is no description provided, the submit button remains inactive
        private void Update()
        {
            if (!_feedbackForm.activeSelf)
            {
                return;
            }

            _sendFeedbackButton.interactable = !string.IsNullOrEmpty(_description.text);
        }

        // Assigned to the `OnClick` on the OpenFeedbackForm's button component
        public void ShowFeedbackForm()
        {
            _openFeedbackButton.SetActive(false);
            _feedbackForm.SetActive(true);
        }

        // Assigned to the `OnClick` on the FeedbackForm's Cancel button component
        public void ResetUserFeedback()
        {
            ResetFormInputs();

            // Hide the FeedbackForm and show the OpenFeedbackButton
            _feedbackForm.SetActive(false);
            _openFeedbackButton.SetActive(true);
        }

        // Assigned to the `OnClick` on the FeedbackForm's SendFeedback button component
        public void SendFeedback()
        {
            if (_addScreenshot.isOn)
            {
                // When adding a screenshot we delay capture until the end of the frame so we can hide the form first
                StartCoroutine(HideFormAndCaptureFeedback());
            }
            else
            {
                // Since there is no screenshot added we can capture the feedback right away
                SentrySdk.CaptureFeedback(_description.text, _email.text, _name.text, addScreenshot: false);
            }
        }

        // This coroutine allows us to hide the FeedbackForm before capturing so it's not visible on the screenshot
        private IEnumerator HideFormAndCaptureFeedback()
        {
            // Hide the feedback form
            _feedbackForm.SetActive(false);

            // We're waiting for the EndOfFrame so the FeedbackForm gets updated before capturing the screenshot
            yield return new WaitForEndOfFrame();

            SentrySdk.CaptureFeedback(_description.text, _email.text, _name.text, addScreenshot: true);

            ResetUserFeedback();
        }

        private void ResetFormInputs()
        {
            if (_name)
            {
                _name.text = string.Empty;
            }

            if (_email)
            {
                _email.text = string.Empty;
            }

            if (_description)
            {
                _description.text = string.Empty;
            }

            if (_addScreenshot)
            {
                _addScreenshot.isOn = true;
            }
        }
    }
}
