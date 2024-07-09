using System.IO;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Tests;

public class UnityViewHierarchyAttachmentTests
{
    private class Fixture
    {
        public SentryUnityOptions Options = new() { AttachViewHierarchy = true };

        public UnityViewHierarchyAttachmentContent GetSut() => new(Options, SentryMonoBehaviour.Instance);
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentryUnity.Close();
        }
    }

    [Test]
    public void GetStream_IsMainThread_ReturnsStream()
    {
        var sut = _fixture.GetSut();

        var stream = sut.GetStream();

        Assert.IsNotNull(stream);
    }

    [Test]
    public void GetStream_IsNonMainThread_ReturnsNullStream()
    {
        var sut = _fixture.GetSut();

        new Thread(() =>
        {
            Thread.CurrentThread.IsBackground = true;

            var stream = sut.GetStream();

            Assert.NotNull(stream);
            Assert.AreEqual(Stream.Null, stream);
        }).Start();
    }

    [Test]
    public void CaptureViewHierarchy_ReturnsNonNullStream()
    {
        var sut = _fixture.GetSut();

        using var stream = sut.CaptureViewHierarchy();

        Assert.AreNotEqual(Stream.Null, stream);
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