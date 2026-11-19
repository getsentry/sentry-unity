using System;
using System.Collections.Generic;
using NUnit.Framework;

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

        // Polled every frame while Unity captures: not a call the tests follow.
        public int TakeFrameRequest(out int width, out int height)
        {
            width = 0;
            height = 0;
            return 0;
        }

        public void DeliverFrame(int request, IntPtr rgba, int width, int height) => Calls.Add("DeliverFrame");
        public void CancelFrameRequest() => Calls.Add("CancelFrameRequest");
    }
}
