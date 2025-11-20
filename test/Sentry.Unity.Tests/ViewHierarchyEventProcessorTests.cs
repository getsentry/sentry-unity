using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Tests;

public class ViewHierarchyEventProcessorTests
{
    private class Fixture
    {
        public SentryUnityOptions Options = new() { AttachViewHierarchy = true };

        public ViewHierarchyEventProcessor GetSut() => new(Options);
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.Close();
        }
    }

    [Test]
    public void Process_IsMainThread_AddsViewHierarchyToHint()
    {
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(1, hint.Attachments.Count);
        Assert.AreEqual(AttachmentType.ViewHierarchy, hint.Attachments.First().Type);
    }

    [Test]
    public void Process_IsNonMainThread_DoesNotAddViewHierarchyToHint()
    {
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;

            var stream = sut.Process(sentryEvent, hint);

            Assert.AreEqual(0, hint.Attachments.Count);
        }).Start();
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void Process_BeforeCaptureViewHierarchyCallbackProvided_RespectViewHierarchyCaptureDecision(bool captureViewHierarchy)
    {
        _fixture.Options.SetBeforeCaptureViewHierarchy(_ => captureViewHierarchy);
        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(captureViewHierarchy ? 1 : 0, hint.Attachments.Count);
    }

    [Test]
    public void SerializeViewHierarchy_ReturnsNonNullOrEmptyByteArray()
    {
        var sut = _fixture.GetSut();
        var viewHierarchy = sut.CreateViewHierarchy(1, 1, 1);

        var byteArray = sut.SerializeViewHierarchy(viewHierarchy);

        Assert.That(byteArray, Is.Not.Null);
        Assert.That(byteArray.Length, Is.GreaterThan(0));
    }

    [Test]
    public void CreateViewHierarchy_CapturesSceneAsRoot()
    {
        var sut = _fixture.GetSut();

        var viewHierarchy = sut.CreateViewHierarchy(1, 1, 1);

        Assert.AreEqual(1, viewHierarchy.Windows.Count); // Sanity check. Root = Currently active scene = 1
        Assert.AreEqual(SceneManager.GetActiveScene().name, viewHierarchy.Windows[0].Type);
    }

    [Test]
    public void CreateViewHierarchy_RootGameObjectCountBiggerThanMaxRootGameObjectCount_PrunesCreatedViewHierarchy()
    {
        var sut = _fixture.GetSut();
        for (var i = 0; i < 5; i++)
        {
            var _ = new GameObject($"GameObject_{i}");
        }

        var viewHierarchy = sut.CreateViewHierarchy(3, 1, 1);

        Assert.AreEqual(1, viewHierarchy.Windows.Count); // Sanity check. Root = Currently active scene = 1
        Assert.NotNull(viewHierarchy.Windows[0].Children);
        Assert.AreEqual(3, viewHierarchy.Windows[0].Children.Count);
    }

    [Test]
    public void CreateViewHierarchy_RootGameObjectCountSmallerThanMaxRootGameObjectCount_CreatesViewHierarchy()
    {
        var sut = _fixture.GetSut();
        for (var i = 0; i < 3; i++)
        {
            var _ = new GameObject($"GameObject_{i}");
        }

        var viewHierarchy = sut.CreateViewHierarchy(100, 1, 1);

        Assert.AreEqual(1, viewHierarchy.Windows.Count); // Sanity check. Root = Currently active scene = 1
        Assert.NotNull(viewHierarchy.Windows[0].Children);

        Assert.AreEqual(SceneManager.GetActiveScene().rootCount, viewHierarchy.Windows[0].Children.Count);
    }

    [Test]
    public void CreateNode_HierarchyDepthBiggerThanMaxDepth_PrunesCreatedViewHierarchy()
    {
        var sut = _fixture.GetSut();
        var testHierarchy = new GameObject("GameObject").transform;
        CreateTestHierarchy(5, 1, testHierarchy);
        var root = new UnityViewHierarchyNode("root");

        sut.CreateNode(3, 1, root, testHierarchy);

        Assert.AreEqual(0, root.Children[0].Children[0].Children[0].Children.Count);
    }

    [Test]
    public void CreateNode_HierarchyDepthSmallerThanMaxDepth_CapturesHierarchy()
    {
        var sut = _fixture.GetSut();
        var testHierarchy = new GameObject("GameObject").transform;
        CreateTestHierarchy(3, 1, testHierarchy);
        var root = new UnityViewHierarchyNode("root");

        sut.CreateNode(5, 1, root, testHierarchy);

        Assert.AreEqual(0, root.Children[0].Children[0].Children[0].Children.Count);
    }

    [Test]
    public void CreateNode_MoreChildrenThanMaxChildCount_PrunesCreatedViewHierarchy()
    {
        var sut = _fixture.GetSut();
        var testHierarchy = new GameObject("GameObject").transform;
        CreateTestHierarchy(2, 5, testHierarchy);

        var root = new UnityViewHierarchyNode("root");
        sut.CreateNode(2, 3, root, testHierarchy);

        Assert.AreEqual(1, root.Children.Count); // Sanity check
        Assert.AreEqual(3, root.Children[0].Children.Count);
    }

    [Test]
    public void CreateNode_LessChildrenThanMaxChildCount_CapturesViewHierarchy()
    {
        var sut = _fixture.GetSut();
        var testHierarchy = new GameObject("GameObject").transform;
        CreateTestHierarchy(2, 3, testHierarchy);
        var root = new UnityViewHierarchyNode("root");

        sut.CreateNode(2, 5, root, testHierarchy);

        Assert.AreEqual(1, root.Children.Count); // Sanity check
        Assert.AreEqual(3, root.Children[0].Children.Count);
    }

    [Test]
    public void Process_BeforeSendViewHierarchyCallback_ReceivesViewHierarchyAndEvent()
    {
        ViewHierarchy? receivedViewHierarchy = null;
        SentryEvent? receivedEvent = null;

        _fixture.Options.SetBeforeSendViewHierarchy((viewHierarchy, @event) =>
        {
            receivedViewHierarchy = viewHierarchy;
            receivedEvent = @event;
            return viewHierarchy;
        });

        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.NotNull(receivedViewHierarchy);
        Assert.NotNull(receivedEvent);
        Assert.AreEqual(sentryEvent.EventId, receivedEvent!.EventId);
        Assert.AreEqual(1, hint.Attachments.Count);
    }

    [Test]
    public void Process_BeforeSendViewHierarchyCallback_ReturnsNull_SkipsAttachment()
    {
        _fixture.Options.SetBeforeSendViewHierarchy((_, _) => null);

        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(0, hint.Attachments.Count);
    }

    [Test]
    public void Process_BeforeSendViewHierarchyCallback_ModifiesHierarchy_UsesModifiedVersion()
    {
        var callbackInvoked = false;

        _fixture.Options.SetBeforeSendViewHierarchy((viewHierarchy, @event) =>
        {
            callbackInvoked = true;
            // Remove all children from the root window
            viewHierarchy.Windows[0].Children.Clear();
            return viewHierarchy;
        });

        var sut = _fixture.GetSut();

        // Create some game objects so there's something to remove
        for (var i = 0; i < 3; i++)
        {
            var _ = new GameObject($"GameObject_{i}");
        }

        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.IsTrue(callbackInvoked);
        Assert.AreEqual(1, hint.Attachments.Count);

        // Verify the modification was applied by deserializing
        var attachment = hint.Attachments.First();
        var content = attachment.Content as ByteAttachmentContent;
        Assert.NotNull(content);

        using var stream = content!.GetStream();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        // The JSON should show an empty children array
        Assert.That(json, Does.Contain("\"children\":[]"));
    }

    [Test]
    public void Process_BeforeSendViewHierarchyCallback_ReturnsDifferentHierarchy_UsesNewHierarchy()
    {
        var newHierarchy = new ViewHierarchy("CustomRenderingSystem");
        newHierarchy.Windows.Add(new UnityViewHierarchyNode("CustomWindow"));

        _fixture.Options.SetBeforeSendViewHierarchy((_, _) => newHierarchy);

        var sut = _fixture.GetSut();
        var sentryEvent = new SentryEvent();
        var hint = new SentryHint();

        sut.Process(sentryEvent, hint);

        Assert.AreEqual(1, hint.Attachments.Count);

        // Verify the new hierarchy was used
        var attachment = hint.Attachments.First();
        var content = attachment.Content as ByteAttachmentContent;
        Assert.NotNull(content);

        using var stream = content!.GetStream();
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        Assert.That(json, Does.Contain("CustomRenderingSystem"));
        Assert.That(json, Does.Contain("CustomWindow"));
    }

    private void CreateTestHierarchy(int remainingDepth, int childCount, Transform parent)
    {
        remainingDepth--;
        if (remainingDepth <= 0)
        {
            return;
        }

        for (var i = 0; i < childCount; i++)
        {
            var gameObject = new GameObject($"{parent.name}_{i}");
            gameObject.transform.SetParent(parent);

            CreateTestHierarchy(remainingDepth, childCount, gameObject.transform);
        }
    }
}
