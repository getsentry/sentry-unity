using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests;

public class SentryMonoBehaviourTests
{
    private class Fixture
    {
        public SentryMonoBehaviour GetSut()
        {
            var gameObject = new GameObject("PauseTest");
            var sentryMonoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();
            sentryMonoBehaviour.Application = new TestApplication();

            return sentryMonoBehaviour;
        }
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [Test]
    public void OnApplicationPause_PauseStatusTrue_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationPause(true);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void OnApplicationFocus_FocusFalse_ApplicationPausingInvoked()
    {
        var wasPausingCalled = false;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => wasPausingCalled = true;

        sut.OnApplicationFocus(false);

        Assert.IsTrue(wasPausingCalled);
    }

    [Test]
    public void UpdatePauseStatus_PausedTwice_ApplicationPausingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationPausing += () => counter++;

        sut.UpdatePauseStatus(true);
        sut.UpdatePauseStatus(true);

        Assert.AreEqual(1, counter);
    }

    [Test]
    public void UpdatePauseStatus_ResumedTwice_ApplicationResumingInvokedOnlyOnce()
    {
        var counter = 0;

        var sut = _fixture.GetSut();
        sut.ApplicationResuming += () => counter++;
        // We need to pause it first to resume it.
        sut.UpdatePauseStatus(true);

        sut.UpdatePauseStatus(false);
        sut.UpdatePauseStatus(false);

        Assert.AreEqual(1, counter);
    }

    [UnityTest]
    public IEnumerator CaptureScreenshotForEvent_Called_CapturesScreenshotAttachmentForEventId()
    {
        // Arrange
        var options = new SentryUnityOptions();
        var sut = _fixture.GetSut();

        var capturedId = SentryId.Empty;
        SentryAttachment? capturedAttachment = null;
        sut.AttachmentCaptureFunction = (eventId, attachment) =>
        {
            capturedId = eventId;
            capturedAttachment = attachment;
        };

        var eventId = SentryId.Create();

        // Act
        sut.CaptureScreenshotForEvent(options, eventId);

        // Wait for the coroutine to complete
        yield return 0;

        // Assert
        Assert.AreEqual(eventId, capturedId);
        Assert.IsNotNull(capturedAttachment); // Sanity check
        Assert.AreEqual("screenshot.jpg", capturedAttachment!.FileName);
        Assert.AreEqual("image/jpeg", capturedAttachment.ContentType);
        Assert.AreEqual(AttachmentType.Default, capturedAttachment.Type);
    }

    [UnityTest]
    public IEnumerator CaptureScreenshotForEvent_CalledMultipleTimesInOneFrame_OnlyExecutesOnce()
    {
        // Arrange
        var options = new SentryUnityOptions();
        var sut = _fixture.GetSut();

        var didScreenshotCaptureGetCalled = false;
        sut.ScreenshotCaptureFunction = _ =>
        {
            didScreenshotCaptureGetCalled = true;
            return [0];
        };

        var didCaptureMethodGetCalled = false;
        var captureMethodCallCount = 0;
        sut.AttachmentCaptureFunction = (_, _) =>
        {
            didCaptureMethodGetCalled = true;
            captureMethodCallCount++;
        };

        var eventId = SentryId.Create();

        // Act
        sut.CaptureScreenshotForEvent(options, eventId);
        sut.CaptureScreenshotForEvent(options, eventId);
        sut.CaptureScreenshotForEvent(options, eventId);

        // Wait for the coroutine to complete
        yield return 0;

        // Assert
        Assert.IsTrue(didScreenshotCaptureGetCalled);
        Assert.IsTrue(didCaptureMethodGetCalled);
        Assert.AreEqual(1, captureMethodCallCount);
    }

    [UnityTest]
    public IEnumerator CaptureScreenshotForEvent_CalledTwoFramesApart_CapturesBothScreenshots()
    {
        // Arrange
        var options = new SentryUnityOptions();
        var sut = _fixture.GetSut();

        var capturedEventIds = new List<SentryId>();
        var capturedAttachments = new List<SentryAttachment>();
        sut.AttachmentCaptureFunction = (eventId, attachment) =>
        {
            capturedEventIds.Add(eventId);
            capturedAttachments.Add(attachment);
        };

        var eventId1 = SentryId.Create();
        var eventId2 = SentryId.Create();

        // Act
        sut.CaptureScreenshotForEvent(options, eventId1);

        // Wait for the first coroutine to complete
        yield return 0;

        // Capture second screenshot in next frame
        sut.CaptureScreenshotForEvent(options, eventId2);

        // Wait for the second coroutine to complete
        yield return 0;

        // Assert
        Assert.AreEqual(2, capturedEventIds.Count);
        Assert.AreEqual(2, capturedAttachments.Count);

        // First screenshot
        Assert.AreEqual(eventId1, capturedEventIds[0]);
        Assert.IsNotNull(capturedAttachments[0]);
        Assert.AreEqual("screenshot.jpg", capturedAttachments[0].FileName);
        Assert.AreEqual("image/jpeg", capturedAttachments[0].ContentType);
        Assert.AreEqual(AttachmentType.Default, capturedAttachments[0].Type);

        // Second screenshot
        Assert.AreEqual(eventId2, capturedEventIds[1]);
        Assert.IsNotNull(capturedAttachments[1]);
        Assert.AreEqual("screenshot.jpg", capturedAttachments[1].FileName);
        Assert.AreEqual("image/jpeg", capturedAttachments[1].ContentType);
        Assert.AreEqual(AttachmentType.Default, capturedAttachments[1].Type);
    }
}
