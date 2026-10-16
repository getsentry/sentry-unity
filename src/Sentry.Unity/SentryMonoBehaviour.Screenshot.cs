using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives taking the screenshots requested by events.
/// </summary>
public partial class SentryMonoBehaviour
{
    private Coroutine? _screenshotCoroutine;

    internal void StartScreenshotCapturer(SentryScreenshotCapturer capturer)
        => StartScreenshotCapturer(capturer, new WaitForEndOfFrame());

    // For testing - WaitForEndOfFrame never resumes in batch mode.
    internal void StartScreenshotCapturer(SentryScreenshotCapturer capturer, YieldInstruction? endOfFrame)
    {
        if (_screenshotCoroutine is not null)
        {
            StopCoroutine(_screenshotCoroutine);
        }

        _screenshotCoroutine = StartCoroutine(ScreenshotCapturerCoroutine(capturer, endOfFrame));
    }

    private static IEnumerator ScreenshotCapturerCoroutine(SentryScreenshotCapturer capturer, YieldInstruction? endOfFrame)
    {
        while (true)
        {
            // Capturing mid-frame yields an incomplete image, so screenshots are only ever taken once the frame has
            // been rendered. This never resumes in headless mode, where waiting for a screenshot times out instead.
            yield return endOfFrame;

            // The SDK can get closed from any thread, so instead of being stopped from the outside the
            // coroutine ends itself. GPU resources can only be released here, on the main thread.
            if (capturer.IsDisposed)
            {
                capturer.ReleaseResources();
                yield break;
            }

            capturer.OnEndOfFrame();
        }
    }
}
