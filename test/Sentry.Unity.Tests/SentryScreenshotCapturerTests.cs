using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Extensibility;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Sentry.Unity.Tests;

public class SentryScreenshotCapturerTests
{
    // Captures a known image instead of the screen. Top half red, bottom half blue, so that a mirrored
    // readback is detectable.
    private class KnownImageCapturer : SentryScreenshotCapturer
    {
        public Texture2D Source { get; set; }

        public int CaptureCount { get; private set; }

        public Vector2Int CaptureSize { get; private set; }

        public Vector2Int? ScreenSize { get; set; }

        internal override (int Width, int Height) GetScreenSize() =>
            ScreenSize is { } size ? (size.x, size.y) : base.GetScreenSize();

        public KnownImageCapturer(SentryUnityOptions options, Texture2D source, bool useAsyncReadback)
            : base(options, useAsyncReadback)
        {
            Source = source;
        }

        internal override void CaptureScreen(RenderTexture target)
        {
            CaptureCount++;
            CaptureSize = new Vector2Int(target.width, target.height);
            Graphics.Blit(Source, target);
        }
    }

    private Texture2D _source = null!;
    private int? _originalMainThreadId;

    [SetUp]
    public void SetUp()
    {
        _originalMainThreadId = MainThreadData.MainThreadId;
        MainThreadData.MainThreadId = Thread.CurrentThread.ManagedThreadId;

        _source = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var pixels = new Color32[64 * 64];
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                // Texture2D rows start at the bottom
                pixels[y * 64 + x] = y >= 32 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 255);
            }
        }
        _source.SetPixels32(pixels);
        _source.Apply();
    }

    [TearDown]
    public void TearDown()
    {
        MainThreadData.MainThreadId = _originalMainThreadId;
        Object.Destroy(_source);
    }

    private static SentryUnityOptions GetOptions() => new() { ScreenshotQuality = ScreenshotQuality.Low };

    private static void RequireAsyncReadback()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }
    }

    private static SentryScreenshotAttachmentContent Request(SentryScreenshotCapturer capturer,
        Func<Texture2D, Texture2D?>? transform = null, Func<SentryScreenshotPixels, bool>? beforeEncode = null)
    {
        var content = capturer.TryRequest(transform, beforeEncode);
        Assert.IsInstanceOf<SentryScreenshotAttachmentContent>(content);
        return (SentryScreenshotAttachmentContent)content!;
    }

    // WaitForEndOfFrame never resumes in batch mode. Capturing the known image doesn't depend on the frame
    // being complete, so taking it on a regular frame is fine here.
    private static IEnumerator TakeScreenshot(SentryScreenshotCapturer capturer, params SentryScreenshotAttachmentContent[] contents)
    {
        yield return null;
        capturer.OnEndOfFrame();

        yield return WaitFor(() => contents.All(content => content.Request.IsCompleted));
    }

    // Reading happens on the SDK's background worker
    private static IEnumerator ReadOnWorker(IAttachmentContent content, Action<byte[]> onRead)
    {
        var task = Task.Run(() => ReadAll(content));
        yield return WaitFor(() => task.IsCompleted, 600);
        onRead(task.Result);
    }

    private static Texture2D CreateSolid(Color32 color)
    {
        var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        texture.SetPixels32(Enumerable.Repeat(color, 64 * 64).ToArray());
        texture.Apply();
        return texture;
    }

    private static byte[] ReadAll(IAttachmentContent content)
    {
        using var stream = content.GetStream();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    private static Texture2D Decode(byte[]? bytes)
    {
        Assert.IsNotNull(bytes);
        Assert.IsNotEmpty(bytes);

        var texture = new Texture2D(2, 2);
        Assert.IsTrue(texture.LoadImage(bytes));
        return texture;
    }

    private static void AssertTopIsRedAndBottomIsBlue(Texture2D image, string label)
    {
        var top = image.GetPixel(image.width / 2, image.height * 3 / 4);
        var bottom = image.GetPixel(image.width / 2, image.height / 4);

        // JPG is lossy, so only the dominant channel is compared
        Assert.Greater(top.r, top.b, $"{label}: expected the top half to be red but got {top}.");
        Assert.Greater(bottom.b, bottom.r, $"{label}: expected the bottom half to be blue but got {bottom}.");
    }

    private static IEnumerator WaitFor(Func<bool> condition, int maxFrames = 300)
    {
        for (var i = 0; i < maxFrames && !condition(); i++)
        {
            yield return null;
        }

        Assert.IsTrue(condition(), "Timed out waiting for the condition.");
    }

    [Test]
    public void TryRequest_DoesNotTakeTheScreenshotRightAway()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);

        var content = Request(capturer);

        Assert.IsTrue(capturer.HasRequest);
        Assert.IsFalse(content.Request.IsCompleted);
        Assert.AreEqual(0, capturer.CaptureCount);
    }

    [UnityTest]
    public IEnumerator TryRequest_ScreenLargerThanTarget_CapturesAtScreenResolutionAndScalesDown()
    {
        // The capture copies the screen into the target without scaling it, so capturing into a target smaller than
        // the screen would crop the screenshot
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false)
        {
            ScreenSize = new Vector2Int(1000, 2000)
        };
        var content = Request(capturer);
        yield return TakeScreenshot(capturer, content);

        byte[] bytes = null!;
        yield return ReadOnWorker(content, read => bytes = read);
        var image = Decode(bytes);

        Assert.AreEqual(new Vector2Int(1000, 2000), capturer.CaptureSize);
        Assert.AreEqual(SentryScreenshot.GetTargetSize(ScreenshotQuality.Low, 1000, 2000),
            (image.width, image.height));
        Object.Destroy(image);
    }

    [UnityTest]
    public IEnumerator TryRequest_SynchronousReadback_ReturnsFrameInOrientationOfTheCapture()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);
        yield return TakeScreenshot(capturer, content);

        byte[] bytes = null!;
        yield return ReadOnWorker(content, read => bytes = read);
        var image = Decode(bytes);

        // The orientation fix on UV-starts-at-top platforms is applied to whatever gets captured, so the
        // known image itself comes out mirrored there. What matters is that both readbacks agree.
        if (SentrySystemInfoAdapter.Instance.GraphicsUVStartsAtTop ?? true)
        {
            FlipVertically(image);
        }

        AssertTopIsRedAndBottomIsBlue(image, "synchronous readback");
        Object.Destroy(image);
    }

    [UnityTest]
    public IEnumerator TryRequest_AsyncReadback_MatchesSynchronousReadback()
    {
        RequireAsyncReadback();

        using var asyncCapturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: true);
        using var syncCapturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var asyncContent = Request(asyncCapturer);
        var syncContent = Request(syncCapturer);
        yield return TakeScreenshot(asyncCapturer, asyncContent);
        yield return TakeScreenshot(syncCapturer, syncContent);

        byte[] asyncBytes = null!;
        byte[] syncBytes = null!;
        yield return ReadOnWorker(asyncContent, read => asyncBytes = read);
        yield return ReadOnWorker(syncContent, read => syncBytes = read);
        var asyncImage = Decode(asyncBytes);
        var syncImage = Decode(syncBytes);

        var asyncTop = asyncImage.GetPixel(asyncImage.width / 2, asyncImage.height * 3 / 4);
        var syncTop = syncImage.GetPixel(syncImage.width / 2, syncImage.height * 3 / 4);
        Assert.AreEqual(syncTop.r > syncTop.b, asyncTop.r > asyncTop.b,
            $"Async readback is mirrored compared to the synchronous one. Async top: {asyncTop}, sync top: {syncTop}.");

        Object.Destroy(asyncImage);
        Object.Destroy(syncImage);
    }

    [UnityTest]
    public IEnumerator TryRequest_DefersEncodeUntilRead()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);
        yield return TakeScreenshot(capturer, content);

        var frame = content.Request.Frame.Result!;
        Assert.IsFalse(frame.IsEncodeStarted, "Taking the screenshot must not encode.");

        // The SDK checks the length when building the envelope. Reporting one would force the wait and the encode.
        using (var stream = content.GetStream())
        {
            Assert.IsFalse(stream.CanSeek);
            Assert.Throws<NotSupportedException>(() => _ = stream.Length);
        }
        Assert.IsFalse(frame.IsEncodeStarted, "Opening the stream must not encode.");

        byte[] bytes = null!;
        yield return ReadOnWorker(content, read => bytes = read);

        Assert.IsTrue(frame.IsEncodeStarted);
        Object.Destroy(Decode(bytes));
    }

    [UnityTest]
    public IEnumerator TryRequest_ReadOnTheWorkerBeforeTaken_WaitsForTheEndOfFrame()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);

        var task = Task.Run(() => ReadAll(content));
        yield return null;
        Assert.IsFalse(task.IsCompleted, "Reading must wait for the screenshot to be taken.");

        capturer.OnEndOfFrame();
        yield return WaitFor(() => task.IsCompleted, 600);

        Object.Destroy(Decode(task.Result));
    }

    [Test]
    public void TryRequest_ReadOnTheMainThreadBeforeTaken_ReturnsEmptyWithoutWaiting()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);

        // Waiting here would deadlock, as taking the screenshot needs the main thread
        Assert.IsEmpty(ReadAll(content));
    }

    [UnityTest]
    public IEnumerator TryRequest_SameFrame_SharesTheScreenshotAndEncodesOnce()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var first = Request(capturer);
        var second = Request(capturer);
        yield return TakeScreenshot(capturer, first, second);

        Assert.AreSame(first.Request, second.Request);
        Assert.AreEqual(1, capturer.CaptureCount);

        byte[] firstBytes = null!;
        byte[] secondBytes = null!;
        yield return ReadOnWorker(first, read => firstBytes = read);
        yield return ReadOnWorker(second, read => secondBytes = read);
        CollectionAssert.AreEqual(firstBytes, secondBytes);
    }

    [UnityTest]
    public IEnumerator TryRequest_WithinMinCaptureInterval_SharesTheLatestScreenshot()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var first = Request(capturer);
        yield return TakeScreenshot(capturer, first);

        var second = Request(capturer);

        Assert.AreSame(first.Request, second.Request);
        Assert.IsFalse(capturer.HasRequest);
    }

    [UnityTest]
    public IEnumerator TryRequest_AfterMinCaptureInterval_TakesANewScreenshot()
    {
        var red = CreateSolid(new Color32(255, 0, 0, 255));
        var green = CreateSolid(new Color32(0, 255, 0, 255));
        using var capturer = new KnownImageCapturer(GetOptions(), red, useAsyncReadback: false);

        try
        {
            var first = Request(capturer);
            yield return TakeScreenshot(capturer, first);

            yield return new WaitForSecondsRealtime((float)SentryScreenshotCapturer.MinCaptureInterval.TotalSeconds * 1.5f);
            capturer.Source = green;
            var second = Request(capturer);
            yield return TakeScreenshot(capturer, second);

            Assert.AreNotSame(first.Request, second.Request);

            // The first screenshot stays as it was taken
            byte[] bytes = null!;
            yield return ReadOnWorker(first, read => bytes = read);
            var image = Decode(bytes);
            var pixel = image.GetPixel(image.width / 2, image.height / 2);
            Assert.Greater(pixel.r, pixel.g, $"Expected the first screenshot to stay red but got {pixel}.");
            Object.Destroy(image);
        }
        finally
        {
            Object.Destroy(red);
            Object.Destroy(green);
        }
    }

    [UnityTest]
    public IEnumerator TryRequest_TooManyPendingScreenshots_SharesTheLatest()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);

        // Kept alive, as screenshots no attachment holds on to anymore don't count
        var pending = new List<SentryScreenshotAttachmentContent>();
        for (var i = 0; i < SentryScreenshotCapturer.MaxPendingRequests; i++)
        {
            yield return new WaitForSecondsRealtime((float)SentryScreenshotCapturer.MinCaptureInterval.TotalSeconds * 1.5f);
            pending.Add(Request(capturer));
            yield return TakeScreenshot(capturer, pending[i]);
        }

        yield return new WaitForSecondsRealtime((float)SentryScreenshotCapturer.MinCaptureInterval.TotalSeconds * 1.5f);
        var content = Request(capturer);

        Assert.AreSame(pending.Last().Request, content.Request);
        Assert.IsFalse(capturer.HasRequest);
        GC.KeepAlive(pending);
    }

    [UnityTest]
    public IEnumerator TryRequest_NotTakenInTime_ResolvesEmptyAndSkipsUntilTheNextEndOfFrame()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);

        var task = Task.Run(() => ReadAll(content));
        var deadline = Time.realtimeSinceStartup + (float)SentryScreenshotCapturer.CaptureTimeout.TotalSeconds * 3;
        while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
        {
            yield return null;
        }

        Assert.IsTrue(task.IsCompleted, "Reading must give up once the screenshot didn't get taken in time.");
        Assert.IsEmpty(task.Result);
        Assert.IsNull(capturer.TryRequest(), "While the main thread doesn't render, no screenshot can be taken.");

        capturer.OnEndOfFrame();

        Assert.IsNotNull(capturer.TryRequest());
    }

    [UnityTest]
    public IEnumerator TryRequest_WithTransform_AppliesItWhenTakenAndEncodesTheResult()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);

        Texture2D? received = null;
        var transformedOnMainThread = false;
        var content = Request(capturer, texture =>
        {
            received = texture;
            transformedOnMainThread = MainThreadData.IsMainThread();
            return texture;
        });
        Assert.IsNull(received, "The transform must wait for the screenshot to be taken.");

        yield return TakeScreenshot(capturer, content);

        Assert.IsNotNull(received);
        Assert.IsTrue(transformedOnMainThread);

        byte[] bytes = null!;
        yield return ReadOnWorker(content, read => bytes = read);
        Object.Destroy(Decode(bytes));
    }

    [UnityTest]
    public IEnumerator TryRequest_TransformDiscardsTheScreenshot_ResolvesEmpty()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var discarded = Request(capturer, _ => null);
        var kept = Request(capturer);
        yield return TakeScreenshot(capturer, discarded, kept);

        byte[] discardedBytes = null!;
        byte[] keptBytes = null!;
        yield return ReadOnWorker(discarded, read => discardedBytes = read);
        yield return ReadOnWorker(kept, read => keptBytes = read);

        Assert.IsEmpty(discardedBytes);
        Assert.IsNotEmpty(keptBytes, "A transform only affects the event it was applied for.");
    }

    [UnityTest]
    public IEnumerator TryRequest_TransformInTheSameFrame_RunsOnceAndSharesTheResult()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var calls = 0;
        Texture2D? Transform(Texture2D texture)
        {
            calls++;
            return texture;
        }

        // Applying and encoding the transform costs the main thread, so a burst of events must not multiply it
        var first = Request(capturer, Transform);
        var second = Request(capturer, Transform);
        yield return TakeScreenshot(capturer, first, second);

        byte[]? firstBytes = null;
        byte[]? secondBytes = null;
        yield return ReadOnWorker(first, bytes => firstBytes = bytes);
        yield return ReadOnWorker(second, bytes => secondBytes = bytes);

        Assert.AreEqual(1, calls);
        Assert.IsNotEmpty(firstBytes);
        CollectionAssert.AreEqual(firstBytes, secondBytes);
    }

    [UnityTest]
    public IEnumerator TryRequest_TransformWithinMinCaptureInterval_SharesTheTransformedScreenshot()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var calls = 0;
        Texture2D? Transform(Texture2D texture)
        {
            calls++;
            return texture;
        }

        var first = Request(capturer, Transform);
        yield return TakeScreenshot(capturer, first);

        var second = Request(capturer, Transform);

        Assert.AreSame(first.Request, second.Request);
        Assert.IsFalse(capturer.HasRequest);
        Assert.AreEqual(1, calls);
    }

    [UnityTest]
    public IEnumerator TryRequest_AsyncReadback_ReturnsTheBufferOnceEncodedAndReusesIt()
    {
        RequireAsyncReadback();
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: true);

        var first = Request(capturer);
        yield return TakeScreenshot(capturer, first);
        Assert.AreEqual(1, capturer.UsedBufferCount, "The frame holds its buffer until encoded.");

        yield return ReadOnWorker(first, bytes => Assert.IsNotEmpty(bytes));
        Assert.AreEqual(0, capturer.UsedBufferCount);
        Assert.AreEqual(1, capturer.FreeBufferCount);

        yield return new WaitForSecondsRealtime((float)SentryScreenshotCapturer.MinCaptureInterval.TotalSeconds + 0.05f);
        var second = Request(capturer);
        Assert.AreNotSame(first.Request, second.Request);
        yield return TakeScreenshot(capturer, second);

        // Reused rather than allocating another one
        Assert.AreEqual(1, capturer.UsedBufferCount);
        Assert.AreEqual(0, capturer.FreeBufferCount);

        byte[]? secondBytes = null;
        yield return ReadOnWorker(second, bytes => secondBytes = bytes);
        var image = Decode(secondBytes);
        Object.Destroy(image);
        Assert.AreEqual(1, capturer.FreeBufferCount);
    }

    [UnityTest]
    public IEnumerator TryRequest_AsyncReadbackWithOnlyATransform_ReturnsTheBufferRightAway()
    {
        RequireAsyncReadback();
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: true);

        var content = Request(capturer, texture => texture);
        yield return TakeScreenshot(capturer, content);

        // The transform already encoded what gets sent, nothing else needs the pixels
        Assert.AreEqual(0, capturer.UsedBufferCount);
        Assert.AreEqual(1, capturer.FreeBufferCount);
        yield return ReadOnWorker(content, bytes => Assert.IsNotEmpty(bytes));
    }

    [UnityTest]
    public IEnumerator TryRequest_BeforeEncode_RunsOnTheWorkerAndItsEditsGetEncoded()
    {
        RequireAsyncReadback();
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: true);
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        int? callbackThreadId = null;

        var content = Request(capturer, beforeEncode: pixels =>
        {
            callbackThreadId = Thread.CurrentThread.ManagedThreadId;

            // Paints the whole screenshot green, as a redaction would paint over an area
            var data = pixels.Data;
            for (var i = 0; i < data.Length; i += 4)
            {
                data[i] = 0;
                data[i + 1] = 255;
                data[i + 2] = 0;
            }

            return true;
        });
        yield return TakeScreenshot(capturer, content);

        Assert.IsNull(callbackThreadId, "The callback must not run before the attachment gets read.");

        byte[]? bytes = null;
        yield return ReadOnWorker(content, read => bytes = read);

        Assert.IsNotNull(callbackThreadId);
        Assert.AreNotEqual(mainThreadId, callbackThreadId);
        var image = Decode(bytes);
        var pixel = image.GetPixel(image.width / 2, image.height / 2);
        Assert.Greater(pixel.g, pixel.r, $"Expected the edited screenshot to be green but got {pixel}.");
        Object.Destroy(image);
    }

    [UnityTest]
    public IEnumerator TryRequest_BeforeEncodeReturnsFalse_ResolvesEmpty()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);

        var content = Request(capturer, beforeEncode: _ => false);
        yield return TakeScreenshot(capturer, content);

        yield return ReadOnWorker(content, bytes => Assert.IsEmpty(bytes));
    }

    [UnityTest]
    public IEnumerator TryRequest_BeforeEncodeThrows_ResolvesEmpty()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);

        // It might have been about to redact something, so the screenshot must not go out as is
        var content = Request(capturer, beforeEncode: _ => throw new InvalidOperationException("test"));
        yield return TakeScreenshot(capturer, content);

        yield return ReadOnWorker(content, bytes => Assert.IsEmpty(bytes));
    }

    [UnityTest]
    public IEnumerator TryRequest_BeforeEncodeInTheSameFrame_RunsOnce()
    {
        using var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var calls = 0;
        bool BeforeEncode(SentryScreenshotPixels _)
        {
            Interlocked.Increment(ref calls);
            return true;
        }

        var first = Request(capturer, beforeEncode: BeforeEncode);
        var second = Request(capturer, beforeEncode: BeforeEncode);
        yield return TakeScreenshot(capturer, first, second);

        yield return ReadOnWorker(first, bytes => Assert.IsNotEmpty(bytes));
        yield return ReadOnWorker(second, bytes => Assert.IsNotEmpty(bytes));
        Assert.AreEqual(1, calls);
    }

    [UnityTest]
    public IEnumerator Dispose_ReleasesPendingRequests()
    {
        var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var content = Request(capturer);

        capturer.Dispose();

        byte[] bytes = null!;
        yield return ReadOnWorker(content, read => bytes = read);
        Assert.IsEmpty(bytes);
        Assert.IsTrue(capturer.IsDisposed);
        Assert.IsNull(capturer.TryRequest());
    }

    [UnityTest]
    public IEnumerator Dispose_StopsTheCoroutine()
    {
        var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var gameObject = new GameObject("ScreenshotCapturerTest");
        var behaviour = gameObject.AddComponent<SentryMonoBehaviour>();

        try
        {
            behaviour.StartScreenshotCapturer(capturer, endOfFrame: null);
            Request(capturer);
            yield return WaitFor(() => capturer.CaptureCount > 0);

            capturer.Dispose();
            yield return null;
            capturer.TryRequest();

            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.AreEqual(1, capturer.CaptureCount);
        }
        finally
        {
            Object.Destroy(gameObject);
        }
    }

    [UnityTest]
    public IEnumerator Dispose_OffTheMainThread_DefersReleaseToTheCoroutine()
    {
        var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: false);
        var gameObject = new GameObject("ScreenshotCapturerTest");
        var behaviour = gameObject.AddComponent<SentryMonoBehaviour>();

        try
        {
            behaviour.StartScreenshotCapturer(capturer, endOfFrame: null);
            Request(capturer);
            yield return WaitFor(() => capturer.CaptureCount > 0);

            // Releasing GPU resources off the main thread throws, which would abort the SDK's Close().
            var task = Task.Run(capturer.Dispose);
            yield return WaitFor(() => task.IsCompleted);

            Assert.IsNull(task.Exception);
            Assert.IsTrue(capturer.IsDisposed);
        }
        finally
        {
            Object.Destroy(gameObject);
        }
    }

    [UnityTest]
    public IEnumerator TryRequest_ThroughTheSdk_ScreenshotRidesAlongTheEvent()
    {
        RequireAsyncReadback();

        var capturer = new KnownImageCapturer(GetOptions(), _source, useAsyncReadback: true);
        var httpHandler = new TestHttpClientHandler("DeferredScreenshotEnvelopeTest");
        var options = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn,
            CreateHttpMessageHandler = () => httpHandler
        };
        options.AddEventProcessor(new ScreenshotEventProcessor(options, capturer));

        SentrySdk.Init(options);

        try
        {
            SentrySdk.CaptureMessage("deferred screenshot");

            // The worker is now waiting for the screenshot
            yield return null;
            capturer.OnEndOfFrame();

            var task = Task.Run(() => httpHandler.GetEvent("screenshot.jpg", TimeSpan.FromSeconds(5)));
            yield return WaitFor(() => task.IsCompleted, 600);

            var request = task.Result;
            StringAssert.Contains("deferred screenshot", request);

            // The item header carries the length of the payload, which is only known once encoded
            var header = Regex.Match(request, @"\{[^\n]*""filename"":""screenshot\.jpg""[^\n]*\}");
            Assert.IsTrue(header.Success, "No screenshot item header found.");
            var length = Regex.Match(header.Value, @"""length"":(\d+)");
            Assert.IsTrue(length.Success, $"No length in the screenshot item header: {header.Value}");
            Assert.Greater(int.Parse(length.Groups[1].Value), 0);
        }
        finally
        {
            SentrySdk.Close();
            capturer.Dispose();
        }
    }

    private static void FlipVertically(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        var flipped = new Color32[pixels.Length];
        for (var y = 0; y < texture.height; y++)
        {
            Array.Copy(pixels, y * texture.width, flipped, (texture.height - 1 - y) * texture.width, texture.width);
        }
        texture.SetPixels32(flipped);
        texture.Apply();
    }
}
