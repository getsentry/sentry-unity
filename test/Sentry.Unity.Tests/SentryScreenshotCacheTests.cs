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

public class SentryScreenshotCacheTests
{
    // Captures a known image instead of the screen. Top half red, bottom half blue, so that a mirrored
    // readback is detectable.
    private class KnownImageCache : SentryScreenshotCache
    {
        public Texture2D Source { get; set; }

        public int RefreshCount { get; private set; }

        public KnownImageCache(SentryUnityOptions options, Texture2D source, bool useAsyncReadback)
            : base(options, useAsyncReadback)
        {
            Source = source;
        }

        internal override void Refresh()
        {
            RefreshCount++;
            base.Refresh();
        }

        internal override void CaptureScreen(RenderTexture target) => Graphics.Blit(Source, target);
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

    // WaitForEndOfFrame never resumes in batch mode. Capturing the known image doesn't depend on the frame
    // being complete, so refreshing on a regular frame is fine here.
    private static IEnumerator RefreshUntilPopulated(SentryScreenshotCache cache)
    {
        yield return null;
        cache.Refresh();

        for (var i = 0; i < 30 && !cache.HasContent; i++)
        {
            yield return null;
        }

        Assert.IsTrue(cache.HasContent, "The cache did not get populated.");
    }

    private static IEnumerator RefreshUntilNewFrame(SentryScreenshotCache cache)
    {
        var previous = cache.CurrentFrame;
        yield return null;
        cache.Refresh();

        for (var i = 0; i < 30 && ReferenceEquals(cache.CurrentFrame, previous); i++)
        {
            yield return null;
        }

        Assert.AreNotSame(previous, cache.CurrentFrame, "The cache did not pick up a new frame.");
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

    [Test]
    public void TryEncodeLatest_CacheEmpty_ReturnsNull()
    {
        var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);

        Assert.IsNull(cache.TryEncodeLatest());
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_SynchronousReadback_ReturnsFrameInOrientationOfTheCapture()
    {
        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(cache);

        var image = Decode(cache.TryEncodeLatest());

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
    public IEnumerator TryEncodeLatest_AsyncReadback_MatchesSynchronousReadback()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var asyncCache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        using var syncCache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(asyncCache);
        yield return RefreshUntilPopulated(syncCache);

        var asyncImage = Decode(asyncCache.TryEncodeLatest());
        var syncImage = Decode(syncCache.TryEncodeLatest());

        var asyncTop = asyncImage.GetPixel(asyncImage.width / 2, asyncImage.height * 3 / 4);
        var syncTop = syncImage.GetPixel(syncImage.width / 2, syncImage.height * 3 / 4);
        Assert.AreEqual(syncTop.r > syncTop.b, asyncTop.r > asyncTop.b,
            $"Async readback is mirrored compared to the synchronous one. Async top: {asyncTop}, sync top: {syncTop}.");

        Object.Destroy(asyncImage);
        Object.Destroy(syncImage);
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_AsyncReadback_EncodesOffTheMainThread()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        var task = Task.Run(() => cache.TryEncodeLatest());
        yield return WaitFor(() => task.IsCompleted);

        Assert.IsNotNull(task.Result);
        Assert.IsNotEmpty(task.Result);
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_SynchronousReadbackOffTheMainThread_ReturnsNull()
    {
        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(cache);

        var task = Task.Run(() => cache.TryEncodeLatest());
        yield return WaitFor(() => task.IsCompleted);

        Assert.IsNull(task.Result);
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_TransformOffTheMainThread_ReturnsNull()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        var transformInvoked = false;
        var task = Task.Run(() => cache.TryEncodeLatest(texture =>
        {
            transformInvoked = true;
            return texture;
        }));
        yield return WaitFor(() => task.IsCompleted);

        Assert.IsNull(task.Result);
        Assert.IsFalse(transformInvoked);
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_SameFrame_EncodesOnce()
    {
        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(cache);

        var first = cache.TryEncodeLatest();
        var second = cache.TryEncodeLatest();

        Assert.IsNotNull(first);
        Assert.AreSame(first, second);
    }

    [UnityTest]
    public IEnumerator TryEncodeLatest_WithTransform_PassesTheFrameAndEncodesTheResult()
    {
        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(cache);

        Texture2D? received = null;
        var bytes = cache.TryEncodeLatest(texture =>
        {
            received = texture;
            return texture;
        });

        Assert.IsNotNull(received);
        Assert.IsNotNull(bytes);
        Assert.IsNotEmpty(bytes);
    }

    [UnityTest]
    public IEnumerator Dispose_ClearsTheCache()
    {
        var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        yield return RefreshUntilPopulated(cache);

        cache.Dispose();
        cache.Refresh();

        Assert.IsTrue(cache.IsDisposed);
        Assert.IsFalse(cache.HasContent);
        Assert.IsNull(cache.TryEncodeLatest());
    }

    [UnityTest]
    public IEnumerator Dispose_StopsTheRefreshCoroutine()
    {
        var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        var gameObject = new GameObject("ScreenshotCacheTest");
        var behaviour = gameObject.AddComponent<SentryMonoBehaviour>();

        try
        {
            behaviour.StartScreenshotCache(cache, System.TimeSpan.Zero, endOfFrame: null);
            yield return WaitFor(() => cache.RefreshCount > 0);

            cache.Dispose();
            yield return null;
            var refreshCount = cache.RefreshCount;

            for (var i = 0; i < 5; i++)
            {
                yield return null;
            }

            Assert.AreEqual(refreshCount, cache.RefreshCount);
        }
        finally
        {
            Object.Destroy(gameObject);
        }
    }

    [UnityTest]
    public IEnumerator Dispose_OffTheMainThread_DefersReleaseToTheRefreshCoroutine()
    {
        var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: false);
        var gameObject = new GameObject("ScreenshotCacheTest");
        var behaviour = gameObject.AddComponent<SentryMonoBehaviour>();

        try
        {
            behaviour.StartScreenshotCache(cache, System.TimeSpan.Zero, endOfFrame: null);
            yield return WaitFor(() => cache.RefreshCount > 0);

            // Releasing GPU resources off the main thread throws, which would abort the SDK's Close().
            var task = Task.Run(cache.Dispose);
            yield return WaitFor(() => task.IsCompleted);

            Assert.IsNull(task.Exception);
            Assert.IsTrue(cache.IsDisposed);
        }
        finally
        {
            Object.Destroy(gameObject);
        }
    }

    private static IEnumerator WaitFor(System.Func<bool> condition, int maxFrames = 300)
    {
        for (var i = 0; i < maxFrames && !condition(); i++)
        {
            yield return null;
        }

        Assert.IsTrue(condition(), "Timed out waiting for the condition.");
    }

    [UnityTest]
    public IEnumerator TryGetLatestContent_AsyncReadback_DefersEncodeUntilRead()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        var content = cache.TryGetLatestContent();

        var deferred = content as SentryScreenshotAttachmentContent;
        Assert.IsNotNull(deferred);
        Assert.IsFalse(deferred!.Frame.IsEncodeStarted, "Attaching must not encode.");

        // The SDK checks the length when building the envelope. Reporting one would force the encode right there.
        using (var stream = content!.GetStream())
        {
            Assert.IsFalse(stream.CanSeek);
            Assert.Throws<NotSupportedException>(() => _ = stream.Length);
        }
        Assert.IsFalse(deferred.Frame.IsEncodeStarted, "Opening the stream must not encode.");

        // Reading happens on the SDK's background worker
        var task = Task.Run(() => ReadAll(content));
        yield return WaitFor(() => task.IsCompleted);

        Assert.IsTrue(deferred.Frame.IsEncodeStarted);
        var image = Decode(task.Result);
        Object.Destroy(image);
    }

    [UnityTest]
    public IEnumerator TryGetLatestContent_SameFrame_EncodesOnce()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        var first = (SentryScreenshotAttachmentContent)cache.TryGetLatestContent()!;
        var second = (SentryScreenshotAttachmentContent)cache.TryGetLatestContent()!;
        ReadAll(first);

        Assert.AreSame(first.Frame, second.Frame);
        Assert.AreSame(first.Frame.GetEncoded(GetOptions()), second.Frame.GetEncoded(GetOptions()));
        CollectionAssert.AreEqual(ReadAll(first), ReadAll(second));
    }

    [UnityTest]
    public IEnumerator TryGetLatestContent_LaterReadbacks_DoNotOverwriteAttachedFrame()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        var red = CreateSolid(new Color32(255, 0, 0, 255));
        var green = CreateSolid(new Color32(0, 255, 0, 255));
        using var cache = new KnownImageCache(GetOptions(), red, useAsyncReadback: true);

        try
        {
            yield return RefreshUntilPopulated(cache);
            var attached = cache.TryGetLatestContent()!;

            // Readbacks reuse buffers of frames nothing holds on to. Two of them cycle through every spare buffer.
            cache.Source = green;
            yield return RefreshUntilNewFrame(cache);
            yield return RefreshUntilNewFrame(cache);

            var image = Decode(ReadAll(attached));
            var pixel = image.GetPixel(image.width / 2, image.height / 2);
            Assert.Greater(pixel.r, pixel.g, $"Expected the attached frame to stay red but got {pixel}.");
            Object.Destroy(image);
        }
        finally
        {
            Object.Destroy(red);
            Object.Destroy(green);
        }
    }

    [UnityTest]
    public IEnumerator TryGetLatestContent_TooManyPendingFrames_EncodesRightAway()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        using var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        // Kept alive, as the cache stops counting frames no attachment holds on to anymore
        var pending = new List<IAttachmentContent>();
        for (var i = 0; i < SentryScreenshotCache.MaxPendingFrames; i++)
        {
            pending.Add(cache.TryGetLatestContent()!);
            Assert.IsInstanceOf<SentryScreenshotAttachmentContent>(pending[i]);
            yield return RefreshUntilNewFrame(cache);
        }

        var content = cache.TryGetLatestContent();

        Assert.IsInstanceOf<ByteAttachmentContent>(content);
        Assert.IsNotEmpty(((ByteAttachmentContent)content!).Bytes);
        GC.KeepAlive(pending);
    }

    [UnityTest]
    public IEnumerator TryGetLatestContent_ThroughTheSdk_EncodedScreenshotRidesAlongTheEvent()
    {
        if (!SystemInfo.supportsAsyncGPUReadback)
        {
            Assert.Ignore("Async GPU readback is not supported on this graphics device.");
        }

        var cache = new KnownImageCache(GetOptions(), _source, useAsyncReadback: true);
        yield return RefreshUntilPopulated(cache);

        var httpHandler = new TestHttpClientHandler("DeferredScreenshotEnvelopeTest");
        var options = new SentryUnityOptions(application: new TestApplication())
        {
            Dsn = SentryTests.TestDsn,
            CreateHttpMessageHandler = () => httpHandler
        };
        options.AddEventProcessor(new ScreenshotEventProcessor(options, cache));

        SentrySdk.Init(options);

        try
        {
            SentrySdk.CaptureMessage("deferred screenshot");

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
            cache.Dispose();
        }
    }

    private static void FlipVertically(Texture2D texture)
    {
        var pixels = texture.GetPixels32();
        var flipped = new Color32[pixels.Length];
        for (var y = 0; y < texture.height; y++)
        {
            System.Array.Copy(pixels, y * texture.width, flipped, (texture.height - 1 - y) * texture.width, texture.width);
        }
        texture.SetPixels32(flipped);
        texture.Apply();
    }
}
