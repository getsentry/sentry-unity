using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Sentry.Unity.iOS.Tests;

public class CocoaReplayTests
{
    private FakeCocoaReplay _replay = null!;
    private SentryUnityOptions _options = null!;

    [SetUp]
    public void SetUp()
    {
        _replay = new FakeCocoaReplay();
        _options = new SentryUnityOptions();
    }

    [TearDown]
    public void TearDown() => _options.NativeSupportCloseCallback?.Invoke();

    [Test]
    public void Use_SessionSampled_StartsSessionReplay()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 0.5;
        _options.Experimental.SessionReplay.OnErrorSampleRate = 1;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0.4);

        CollectionAssert.AreEqual(new[] { "Start" }, _replay.Calls);
    }

    [Test]
    public void Use_SessionNotSampledWithOnErrorRate_BuffersReplay()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 0.5;
        _options.Experimental.SessionReplay.OnErrorSampleRate = 0.1;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0.5);

        CollectionAssert.AreEqual(new[] { "StartBuffering" }, _replay.Calls);
    }

    [Test]
    public void Use_SessionNotSampledWithoutOnErrorRate_StartsNothing()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 0.5;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0.9);

        CollectionAssert.IsEmpty(_replay.Calls);
    }

    [Test]
    public void Use_SetsCocoaReplaySession()
    {
        _options.Experimental.SessionReplay.OnErrorSampleRate = 1;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        Assert.IsInstanceOf<CocoaReplaySession>(_options.ReplaySession);
    }

    [Test]
    public void ShortBackground_PausesAndResumesReplay()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.AutoSessionTrackingInterval = TimeSpan.FromHours(1);
        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        SentryMonoBehaviour.Instance.UpdatePauseStatus(true);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(false);

        CollectionAssert.AreEqual(new[] { "Start", "Pause", "Resume" }, _replay.Calls);
    }

    [Test]
    public void LongBackground_StopsReplayAndSamplesAnother()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 0.5;
        _options.Experimental.SessionReplay.OnErrorSampleRate = 1;
        _options.AutoSessionTrackingInterval = TimeSpan.Zero;
        var samples = new Queue<double>(new[] { 0.1, 0.9 });
        CocoaReplayLifecycle.Use(_options, _replay, samples.Dequeue);

        SentryMonoBehaviour.Instance.UpdatePauseStatus(true);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(false);

        CollectionAssert.AreEqual(new[] { "Start", "Pause", "Stop", "StartBuffering" }, _replay.Calls);
    }

    [Test]
    public void Close_StopsReplayAndUnsubscribes()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        _options.NativeSupportCloseCallback?.Invoke();
        _options.NativeSupportCloseCallback = null;
        SentryMonoBehaviour.Instance.UpdatePauseStatus(true);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(false);

        CollectionAssert.AreEqual(new[] { "Start", "Stop" }, _replay.Calls);
    }

    [Test]
    public void Use_NativeCapture_LeavesCaptureToCocoa()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.Experimental.SessionReplay.Capture = SessionReplayCapture.Native;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(true);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(false);

        CollectionAssert.AreEqual(new[] { "Start", "Pause", "Resume" }, _replay.Calls);
    }

    [Test]
    public void Use_UnityCapture_InstallsUnityCaptureBeforeTheReplayStarts()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.Experimental.SessionReplay.Capture = SessionReplayCapture.Unity;

        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start" }, _replay.Calls);
    }

    [Test]
    public void UnityCapture_Background_CancelsFrameRequestBeforePausing()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.Experimental.SessionReplay.Capture = SessionReplayCapture.Unity;
        _options.AutoSessionTrackingInterval = TimeSpan.FromHours(1);
        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        SentryMonoBehaviour.Instance.UpdatePauseStatus(true);
        SentryMonoBehaviour.Instance.UpdatePauseStatus(false);

        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start", "CancelFrameRequest", "Pause", "Resume" },
            _replay.Calls);
    }

    [Test]
    public void UnityCapture_Close_CancelsFrameRequestAndStopsReplay()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.Experimental.SessionReplay.Capture = SessionReplayCapture.Unity;
        CocoaReplayLifecycle.Use(_options, _replay, () => 0);

        _options.NativeSupportCloseCallback?.Invoke();
        _options.NativeSupportCloseCallback = null;

        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start", "CancelFrameRequest", "Stop" }, _replay.Calls);
    }

    [UnityTest]
    public IEnumerator UnityCapture_TakingFrameRequestFails_CancelsItAndStopsCapturing()
    {
        _replay.TakeFrameRequestThrows = true;
        UseUnityCapture();

        yield return null;
        yield return null;
        yield return null;

        Assert.AreEqual(1, _replay.FrameRequestsPolled);
        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start", "CancelFrameRequest" }, _replay.Calls);
    }

    [UnityTest]
    public IEnumerator UnityCapture_FrameRequestWithoutSize_IsCancelledWithoutCapturing()
    {
        _replay.FrameRequests.Enqueue((1, 0, 0));
        UseUnityCapture();

        yield return null;
        yield return null;
        yield return null;

        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start", "CancelFrameRequest" }, _replay.Calls);
        Assert.Greater(_replay.FrameRequestsPolled, 1);
    }

    [UnityTest]
    public IEnumerator UnityCapture_HostDisabled_CancelsFrameRequestUntilEnabled()
    {
        UseUnityCapture();
        yield return null;
        var host = SentryMonoBehaviour.Instance.GetComponents<UnityReplayRecorderHost>()
            .Single(component => component.Recorder is not null);

        host.enabled = false;
        var polled = _replay.FrameRequestsPolled;
        yield return null;
        yield return null;

        CollectionAssert.AreEqual(new[] { "UseUnityCapture", "Start", "CancelFrameRequest" }, _replay.Calls);
        Assert.AreEqual(polled, _replay.FrameRequestsPolled);

        host.enabled = true;
        yield return null;
        yield return null;

        Assert.Greater(_replay.FrameRequestsPolled, polled);
    }

    [Test]
    public void CocoaReplaySession_CaptureReplay_ReturnsReplayId()
    {
        var replayId = SentryId.Create();
        _replay.Captured = true;
        _replay.ReplayId = replayId.ToString();
        var sut = new CocoaReplaySession(_replay);

        Assert.AreEqual(replayId, sut.CaptureReplay(new SentryEvent()));
        CollectionAssert.AreEqual(new[] { "Capture" }, _replay.Calls);
    }

    [Test]
    public void CocoaReplaySession_NotCaptured_ReturnsNull()
    {
        _replay.Captured = false;
        _replay.ReplayId = SentryId.Create().ToString();
        var sut = new CocoaReplaySession(_replay);

        Assert.IsNull(sut.CaptureReplay(new SentryEvent()));
    }

    [Test]
    public void CocoaReplaySession_ActiveReplayId_ReadsNativeScope()
    {
        var replayId = SentryId.Create();
        _replay.ReplayId = replayId.ToString();
        var sut = new CocoaReplaySession(_replay);

        Assert.AreEqual(replayId, sut.ActiveReplayId);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("00000000000000000000000000000000")]
    public void CocoaReplaySession_NoReplay_ReturnsNull(string? nativeReplayId)
    {
        _replay.Captured = true;
        _replay.ReplayId = nativeReplayId;
        var sut = new CocoaReplaySession(_replay);

        Assert.IsNull(sut.CaptureReplay(new SentryEvent()));
        Assert.IsNull(sut.ActiveReplayId);
    }

    private void UseUnityCapture()
    {
        _options.Experimental.SessionReplay.SessionSampleRate = 1;
        _options.Experimental.SessionReplay.Capture = SessionReplayCapture.Unity;
        CocoaReplayLifecycle.Use(_options, _replay, () => 0);
    }

    private class FakeCocoaReplay : ICocoaReplay
    {
        public List<string> Calls { get; } = new();
        public bool Captured { get; set; }
        public string? ReplayId { get; set; }

        public void Start() => Calls.Add("Start");
        public void StartBuffering() => Calls.Add("StartBuffering");
        public void Pause() => Calls.Add("Pause");
        public void Resume() => Calls.Add("Resume");
        public void Stop() => Calls.Add("Stop");

        public bool Capture()
        {
            Calls.Add("Capture");
            return Captured;
        }

        public string? GetReplayId() => ReplayId;

        public void UseUnityCapture() => Calls.Add("UseUnityCapture");

        // Requests sentry-cocoa makes, as (id, width, height).
        public Queue<(int, int, int)> FrameRequests { get; } = new();
        public bool TakeFrameRequestThrows { get; set; }
        public int FrameRequestsPolled { get; private set; }

        // Polled every frame while Unity captures: counted, not one of the calls the tests follow.
        public int TakeFrameRequest(out int width, out int height)
        {
            FrameRequestsPolled++;
            if (TakeFrameRequestThrows)
            {
                throw new InvalidOperationException("Taking the frame request failed.");
            }

            var (request, requestWidth, requestHeight) = FrameRequests.Count > 0 ? FrameRequests.Dequeue() : (0, 0, 0);
            width = requestWidth;
            height = requestHeight;
            return request;
        }

        public void DeliverFrame(int request, IntPtr rgba, int width, int height) => Calls.Add("DeliverFrame");
        public void CancelFrameRequest() => Calls.Add("CancelFrameRequest");
    }
}
