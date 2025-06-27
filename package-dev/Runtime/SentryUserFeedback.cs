using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Sentry.Unity
{
    public class SentryUserFeedback : MonoBehaviour
    {
        // Form
        [SerializeField] private GameObject _feedbackForm;

        // Buttons
        [SerializeField] private GameObject _openFeedbackButton;
        [SerializeField] private Button _sendBugReportButton;
        [SerializeField] private Button _cancelButton;

        // Inputs
        [SerializeField] private InputField _name;
        [SerializeField] private InputField _email;
        [SerializeField] private InputField _message;
        [SerializeField] private Toggle _addScreenshot;

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

            if (!_name)
            {
                Debug.LogError("Name input field is missing", this);
                gameObject.SetActive(false);
            }

            if (!_email)
            {
                Debug.LogError("Email input field is missing", this);
                gameObject.SetActive(false);
            }

            if (!_message)
            {
                Debug.LogError("Message input field is missing", this);
                gameObject.SetActive(false);
            }

            if (!_addScreenshot)
            {
                Debug.LogError("Add Screenshot toggle is missing", this);
                gameObject.SetActive(false);
            }
        }

        public void ShowFeedbackForm()
        {
            _openFeedbackButton.SetActive(false);
            _feedbackForm.SetActive(true);
        }

        public void HideFeedbackForm()
        {
            // Resetting the form
            _name.text = string.Empty;
            _email.text = string.Empty;
            _message.text = string.Empty;
            _addScreenshot.isOn = true;

            _feedbackForm.SetActive(false);
            _openFeedbackButton.SetActive(true);
        }

        public void SendBugReport() => StartCoroutine(CaptureFeedback());

        private IEnumerator CaptureFeedback()
        {
            // Hide the feedback form
            _feedbackForm.SetActive(false);

            // We're waiting for the EndOfFrame so we can hide the feedback form before capturing the screenshot
            yield return new WaitForEndOfFrame();

            SentryUnity.CaptureFeedback(_message.text, _email.text, _name.text, _addScreenshot);

            HideFeedbackForm();
        }
    }
}
