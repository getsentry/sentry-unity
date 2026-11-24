using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>The user feedback samples: the widget and feedback from code.</summary>
    public static class FeedbackSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Feedback",
            new SampleEntry("feedback/widget", "Open the feedback widget",
                "Opens the SentryUserFeedback form, the one behind the megaphone button.",
                OpenFeedbackWidget),
            new SampleEntry("feedback/capture", "Capture feedback from code",
                "SentrySdk.CaptureFeedback with a message, an email and a name.",
                CaptureFeedback));

        private static void OpenFeedbackWidget()
        {
            foreach (var feedback in Resources.FindObjectsOfTypeAll<SentryUserFeedback>())
            {
                if (feedback.gameObject.scene.IsValid())
                {
                    feedback.ShowFeedbackForm();
                    SampleRunner.Report("Feedback form open");
                    return;
                }
            }

            SampleRunner.Report("This scene has no SentryUserFeedback");
        }

        private static void CaptureFeedback()
        {
            var id = SentrySdk.CaptureFeedback("The bugs are too friendly 🐞", "ant@farm.bug", "ant");
            SampleRunner.Report($"Feedback sent: {id}");
        }
    }
}
