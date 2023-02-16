using System.Collections;
using System.IO;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.TestBehaviours;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public class ViewHierarchyAttachmentTests : DisabledSelfInitializationTests
    {
        private class Fixture
        {
            public SentryUnityOptions Options = new() { AttachViewHierarchy = true };

            public UnityViewHierarchyAttachmentContent GetSut() => new(Options, SentryMonoBehaviour.Instance);
        }

        private Fixture _fixture = null!;

        [SetUp]
        public new void Setup() => _fixture = new Fixture();

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
            // yield return null;

            var sut = _fixture.GetSut();
            using var stream = sut.CaptureViewHierarchy();

            Assert.AreNotEqual(Stream.Null, stream);
        }

        [UnityTest]
        [TestCase(10, 1, ExpectedResult = null)]
        [TestCase(10, 10, ExpectedResult = null)]
        public IEnumerator CreateViewHierarchy_MoreRootGameObjectsThanMax_OnlyCapturesMaxCount(int rootGameObjects, int maxRootGameObjectCount)
        {
            for (var i = 0; i < rootGameObjects; i++)
            {
                var _ = new GameObject($"ViewHierarchyTestObject_{i}");
            }

            yield return null;

            var sut = _fixture.GetSut();

            var viewHierarchy = sut.CreateViewHierarchy(maxRootGameObjectCount, 1, 1);

            Assert.AreEqual(1, viewHierarchy.Windows.Count); // Sanity check. Root = Currently active scene = 1
            Assert.NotNull(viewHierarchy.Windows[0].Children);
            Assert.AreEqual(maxRootGameObjectCount, viewHierarchy.Windows[0].Children!.Count);
        }
    }
}
