using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Sentry.Unity
{
    public class SentryUserFeedbackMonoBehaviour : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;

        [SerializeField] private Text _name;
        [SerializeField] private Text _email;
        [SerializeField] private Text _message;
        [SerializeField] private Toggle _addScreenshot;
        [SerializeField] private Button _sendBugReportButton;
        [SerializeField] private Button _cancelButton;

        private void Awake()
        {
            _sendBugReportButton.onClick.AddListener(SendBugReport);
            _cancelButton.onClick.AddListener(Cancel);
        }

        public void SendBugReport()
        {
            StartCoroutine(CaptureFeedback());
        }

        private IEnumerator CaptureFeedback()
        {
            // Hide the UI first
            _canvas.enabled = false;

            // We're waiting here so we can get rid of the feedback form before capturing the screenshot
            yield return new WaitForEndOfFrame();

            SentryUnity.CaptureFeedback(_message.text, _email.text, _name.text, _addScreenshot);
            Destroy(gameObject);
        }

        public void Cancel() => Destroy(gameObject);
    }
}
