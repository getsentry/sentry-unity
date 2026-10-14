using System;
using System.Collections;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Drives the refresh of the screenshot cache.
/// </summary>
public partial class SentryMonoBehaviour
{
    private Coroutine? _screenshotCoroutine;

    internal void StartScreenshotCache(SentryScreenshotCache cache, TimeSpan interval)
        => StartScreenshotCache(cache, interval, new WaitForEndOfFrame());

    // For testing - WaitForEndOfFrame never resumes in batch mode.
    internal void StartScreenshotCache(SentryScreenshotCache cache, TimeSpan interval, YieldInstruction? endOfFrame)
    {
        if (_screenshotCoroutine is not null)
        {
            StopCoroutine(_screenshotCoroutine);
        }

        _screenshotCoroutine = StartCoroutine(ScreenshotCacheCoroutine(cache, interval, endOfFrame));
    }

    private static IEnumerator ScreenshotCacheCoroutine(SentryScreenshotCache cache, TimeSpan interval, YieldInstruction? endOfFrame)
    {
        // Reusing a WaitForSecondsRealtime is not an option - it captures its deadline on construction,
        // so every wait after the first returns immediately. Tracking the deadline here avoids both that
        // and an allocation per refresh.
        var intervalSeconds = (float)interval.TotalSeconds;
        var nextRefresh = 0f;

        while (true)
        {
            // Capturing mid-frame yields an incomplete image, so the cache is only ever refreshed once
            // the frame has been rendered. This never resumes in headless mode, which leaves the cache
            // empty rather than holding a blank image.
            yield return endOfFrame;

            // The SDK can get closed from any thread, so instead of being stopped from the outside the
            // coroutine ends itself. GPU resources can only be released here, on the main thread.
            if (cache.IsDisposed)
            {
                cache.ReleaseResources();
                yield break;
            }

            if (Time.realtimeSinceStartup < nextRefresh)
            {
                continue;
            }

            cache.Refresh();
            nextRefresh = Time.realtimeSinceStartup + intervalSeconds;
        }
    }
}
