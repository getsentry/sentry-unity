using Unity.Collections;

namespace Sentry.Unity;

/// <summary>
/// The pixels of a screenshot before it gets compressed and attached to an event.
/// </summary>
/// <remarks>
/// Only valid for the duration of the callback it gets handed to - don't hold on to <see cref="Data"/> beyond it.
/// </remarks>
public readonly struct SentryScreenshotPixels
{
    internal SentryScreenshotPixels(NativeArray<byte> data, int width, int height)
    {
        Data = data;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// The width of the screenshot in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// The height of the screenshot in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// The pixels in RGBA32: four bytes per pixel - red, green, blue and alpha - with rows ordered from the bottom of the
    /// screen to the top. Edits made to it end up in the attached screenshot.
    /// </summary>
    public NativeArray<byte> Data { get; }
}
