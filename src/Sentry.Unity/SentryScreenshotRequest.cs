using System;
using System.Threading.Tasks;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// A screenshot requested by one or more events, taken at the end of the frame they were captured in.
/// </summary>
/// <remarks>
/// Attachments hold on to the request and wait for it only once the SDK serializes their envelope on its background
/// worker. Completion runs on the main thread, so continuations are forced off of it.
/// </remarks>
internal sealed class SentryScreenshotRequest
{
    private readonly TaskCompletionSource<SentryScreenshotFrame?> _frame =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    // Set under the capturer's lock until taken, read on the main thread after
    private Func<Texture2D, Texture2D?>? _transform;
    private TaskCompletionSource<byte[]?>? _transformed;
    private volatile bool _hasFrameConsumers;

    /// <summary>
    /// When the frame was taken, in <see cref="System.Diagnostics.Stopwatch"/> ticks. Zero until then.
    /// </summary>
    internal long TakenTimestamp { get; set; }

    /// <summary>
    /// Whether an attachment encodes the frame itself, as opposed to only through the transform.
    /// </summary>
    internal bool HasFrameConsumers
    {
        get => _hasFrameConsumers;
        set => _hasFrameConsumers = value;
    }

    /// <summary>
    /// Whether a transform got registered, so its result can be shared.
    /// </summary>
    internal bool HasTransform => _transformed is not null;

    /// <summary>
    /// Applied to the pixels on the SDK's worker right before the frame gets encoded. Like the transform, it runs once
    /// per screenshot: the first event's callback gets applied.
    /// </summary>
    internal Func<SentryScreenshotPixels, bool>? BeforeEncode { get; private set; }

    /// <summary>
    /// Registers the callback applied to the pixels before encoding, unless one already is. Must be called under the
    /// capturer's lock.
    /// </summary>
    internal void TrySetBeforeEncode(Func<SentryScreenshotPixels, bool> beforeEncode) => BeforeEncode ??= beforeEncode;

    internal Task<SentryScreenshotFrame?> Frame => _frame.Task;

    internal bool IsCompleted => _frame.Task.IsCompleted;

    /// <summary>
    /// Whether the request no longer holds on to memory waiting for an encode.
    /// </summary>
    internal bool IsSettled => _frame.Task.IsCompleted &&
                               (!HasFrameConsumers || _frame.Task.Result is not { } frame || frame.IsEncodeStarted);

    /// <summary>
    /// Registers a transform that gets applied, and its result encoded, on the main thread once the frame is taken.
    /// Must be called under the capturer's lock.
    /// </summary>
    /// <remarks>
    /// Applying and encoding it costs the main thread, so it happens once per screenshot: the first event's transform
    /// gets applied and every event attaching the screenshot shares the result. Returns null once it's too late to
    /// register one, i.e. the frame was taken without.
    /// </remarks>
    internal Task<byte[]?>? TryAddTransform(Func<Texture2D, Texture2D?> transform)
    {
        if (_transformed is { } transformed)
        {
            return transformed.Task;
        }

        if (TakenTimestamp != 0)
        {
            return null;
        }

        _transform = transform;
        _transformed = new TaskCompletionSource<byte[]?>(TaskCreationOptions.RunContinuationsAsynchronously);
        return _transformed.Task;
    }

    /// <summary>
    /// Publishes the frame. Must run on the main thread, as it applies the transforms.
    /// </summary>
    internal void Complete(SentryScreenshotFrame frame, SentryUnityOptions options)
    {
        // The transform completes before the frame does, so that anything waiting on the frame finds it done and no
        // encode can release the pixels from under it.
        if (_transform is not null)
        {
            _transformed!.TrySetResult(ApplyTransform(frame, _transform, options));

            // Nothing encodes the frame itself, so its pixels - possibly a pooled buffer - can go right away
            if (!HasFrameConsumers)
            {
                frame.ReleasePixels();
            }
        }

        // Abandoned in the meantime, e.g. because the worker stopped waiting. Nothing will encode the frame.
        if (!_frame.TrySetResult(frame))
        {
            frame.ReleasePixels();
        }
    }

    /// <summary>
    /// Releases everything waiting on the request without a screenshot.
    /// </summary>
    internal void Abandon()
    {
        _transformed?.TrySetResult(null);
        _frame.TrySetResult(null);
    }

    private static byte[]? ApplyTransform(SentryScreenshotFrame frame, Func<Texture2D, Texture2D?> transform,
        SentryUnityOptions options)
    {
        Texture2D? screenshot = null;
        try
        {
            screenshot = new Texture2D(frame.Width, frame.Height, TextureFormat.RGBA32, false);
            if (!frame.TryLoadInto(screenshot))
            {
                return null;
            }

            screenshot.Apply();

            var transformed = transform.Invoke(screenshot);
            if (transformed == null)
            {
                options.LogInfo("Screenshot discarded by BeforeSendScreenshot callback.");
                return null;
            }

            if (transformed != screenshot)
            {
                UnityEngine.Object.Destroy(screenshot);
                screenshot = transformed;
            }

            var bytes = screenshot.EncodeToJPG(options.ScreenshotCompression);
            return bytes is { Length: > 0 } ? bytes : null;
        }
        catch (Exception e)
        {
            options.LogError(e, "The BeforeSendScreenshot callback threw an exception. Skipping the screenshot.");
            return null;
        }
        finally
        {
            if (screenshot != null)
            {
                UnityEngine.Object.Destroy(screenshot);
            }
        }
    }
}
