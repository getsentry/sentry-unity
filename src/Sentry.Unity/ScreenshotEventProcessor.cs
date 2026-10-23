using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

public class ScreenshotEventProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryUnityOptions _options;
    private readonly ISentryMonoBehaviour _sentryMonoBehaviour;

    // Events share a screenshot until it's done, so frames can't pile up behind a slow encoder
    private readonly object _lock = new();
    private TaskCompletionSource<byte[]?>? _pending;
    private DateTime _pendingSince;
    private List<SentryEvent> _pendingEvents = new();

    public ScreenshotEventProcessor(SentryUnityOptions sentryOptions) : this(sentryOptions, SentryMonoBehaviour.Instance) { }

    internal ScreenshotEventProcessor(SentryUnityOptions sentryOptions, ISentryMonoBehaviour sentryMonoBehaviour)
    {
        _options = sentryOptions;
        _sentryMonoBehaviour = sentryMonoBehaviour;
    }

    public SentryEvent? Process(SentryEvent @event) => @event;

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        // Feedback attaches a screenshot only when asked to, see SentryUnitySdk.CaptureFeedback
        if (@event.Contexts.Feedback is not null)
        {
            return @event;
        }

        try
        {
            if (_options.BeforeCaptureScreenshotInternal?.Invoke(@event) is false)
            {
                return @event;
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "The BeforeCaptureScreenshot callback threw an exception. Skipping the screenshot.");
            return @event;
        }

        Task<byte[]?> screenshot;
        lock (_lock)
        {
            // A capture that never completes, e.g. while the main thread hangs, must not hold up later events
            if (_pending is null || DateTime.UtcNow - _pendingSince > SentryScreenshotAttachmentContent.CaptureTimeout)
            {
                // Its envelopes are past their deadline, so it doesn't need taking anymore
                _pending?.TrySetResult(null);
                _pending = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
                _pendingSince = DateTime.UtcNow;
                _pendingEvents = new List<SentryEvent>();
                _sentryMonoBehaviour.QueueCoroutine(CaptureScreenshotCoroutine(_pending, _pendingEvents));
            }

            _pendingEvents.Add(@event);

            screenshot = _pending.Task;
        }

        // The screenshot gets read when the SDK serializes the event's envelope on its worker
        hint.Attachments.Add(new SentryAttachment(
            AttachmentType.Default,
            new SentryScreenshotAttachmentContent(screenshot, _options),
            "screenshot.jpg",
            "image/jpeg"));

        return @event;
    }

    private IEnumerator CaptureScreenshotCoroutine(TaskCompletionSource<byte[]?> screenshot, List<SentryEvent> events)
    {
        // Capturing mid-frame yields an incomplete image
        yield return WaitForEndOfFrame();

        if (screenshot.Task.IsCompleted)
        {
            yield break;
        }

        // Event processors run before BeforeSend and sampling, which may have dropped the events
        SentryEvent? @event;
        lock (_lock)
        {
            @event = events.Find(e => e.IsCaptured);
        }

        if (@event is null)
        {
            _options.LogDebug("Skipping the screenshot. None of its events got captured.");
            Complete(screenshot, null);
            yield break;
        }

        try
        {
            // The Texture2D callback and the main thread worker need the screenshot right away
            if (_options.BeforeSendScreenshotInternal is null && CanReadBackAsync())
            {
                SentryScreenshot.CaptureAsync(_options, result => Complete(screenshot, result));
            }
            else
            {
                Complete(screenshot, CaptureScreenshot(@event));
            }
        }
        catch (Exception e)
        {
            _options.LogError(e, "Failed to capture screenshot.");
            Complete(screenshot, null);
        }
    }

    private void Complete(TaskCompletionSource<byte[]?> screenshot, byte[]? result)
    {
        lock (_lock)
        {
            if (_pending == screenshot)
            {
                _pending = null;
            }
        }

        screenshot.TrySetResult(result);
    }

    private byte[]? CaptureScreenshot(SentryEvent @event)
    {
        Texture2D? screenshot = null;
        try
        {
            screenshot = CreateNewScreenshotTexture2D(_options);

            if (_options.BeforeSendScreenshotInternal != null)
            {
                var modifiedScreenshot = _options.BeforeSendScreenshotInternal(screenshot, @event);

                if (modifiedScreenshot == null)
                {
                    _options.LogInfo("Screenshot discarded by BeforeSendScreenshot callback.");
                    return null;
                }

                // Clean up - If the user returned a new texture object and did not modify the passed in one
                if (modifiedScreenshot != screenshot)
                {
                    _options.LogDebug("Applying modified screenshot.");
                    UnityEngine.Object.Destroy(screenshot);
                    screenshot = modifiedScreenshot;
                }
            }

            return screenshot.EncodeToJPG(_options.ScreenshotCompression);
        }
        finally
        {
            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }

    internal virtual bool CanReadBackAsync()
        => SystemInfo.supportsAsyncGPUReadback && _options.BackgroundWorker is not WebBackgroundWorker;

    internal virtual Texture2D CreateNewScreenshotTexture2D(SentryUnityOptions options)
        => SentryScreenshot.CreateNewScreenshotTexture2D(options);

    internal virtual YieldInstruction? WaitForEndOfFrame()
        => new WaitForEndOfFrame();
}
