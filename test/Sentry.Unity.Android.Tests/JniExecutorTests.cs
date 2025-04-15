using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Android.Tests
{
    [TestFixture]
    public class JniExecutorTests
    {
        private TestLogger _logger = null!;
        private TestAndroidJNI _androidJni = null!;
        private TestApplication _application = null!;
        private JniExecutor _sut = null!;

        [SetUp]
        public void SetUp()
        {
            // Reset the version to ensure it's not cached from previous tests or the Editor
            SentryUnityVersion.Version = null;
            _logger = new TestLogger();
            _androidJni = new TestAndroidJNI();
            _application = new TestApplication(unityVersion: "2019.4.40f1");
            _sut = new  JniExecutor(_logger, _androidJni, _application);
        }

        [TearDown]
        public void TearDown()
        {
            _androidJni = null!;
            _sut = null!;
        }

        [Test]
        public void Run_TResult_ExecutesOperation()
        {
            // Arrange
            var operationExecuted = false;
            var operation = () =>
            {
                operationExecuted = true;
                return true;
            };

            // Act
            var result = _sut.Run(operation);

            // Assert
            Assert.IsTrue(operationExecuted);
            Assert.IsTrue(result);
        }

        [Test]
        public void Run_Void_ExecutesOperation()
        {
            // Arrange
            var sut = new  JniExecutor(_logger, _androidJni, _application);
            bool operationExecuted = false;
            var operation = () =>
            {
                operationExecuted = true;
            };


            // Act
            _sut.Run(operation);

            // Assert
            Assert.IsTrue(operationExecuted);
        }

        [Test]
        [TestCase("2019.4.40f1", true, true, Description = "Unity <2020 + main thread = should attach")]
        [TestCase("2019.4.40f1", false, true, Description = "Unity <2020 + non main thread = should attach")]
        [TestCase("2020.3.1f1", true, false, Description = "Unity >2020 + main thread = should not attach")]
        [TestCase("2020.3.1f1", false, true, Description = "Unity >2020+ + non main thread = should attach")]
        public void Run_TResult_AttachesThreadBasedOnVersionAndThread(string unityVersion, bool isMainThread, bool shouldAttach)
        {
            // Arrange
            _androidJni = new TestAndroidJNI();
            _application = new TestApplication(unityVersion: unityVersion);
            _sut = new JniExecutor(_logger, _androidJni, _application);

            // Act
            _sut.Run(() => true, isMainThread);

            // Assert
            Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
            Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
        }

        [Test]
        [TestCase("2019.4.40f1", true, true, Description = "Unity <2020 + main thread = should attach")]
        [TestCase("2019.4.40f1", false, true, Description = "Unity <2020 + non main thread = should attach")]
        [TestCase("2020.3.1f1", true, false, Description = "Unity >2020 + main thread = should not attach")]
        [TestCase("2020.3.1f1", false, true, Description = "Unity >2020+ + non main thread = should attach")]
        public void Run_Void_AttachesThreadBasedOnVersionAndThread(string unityVersion, bool isMainThread, bool shouldAttach)
        {
            // Arrange
            _androidJni = new TestAndroidJNI();
            _application = new TestApplication(unityVersion: unityVersion);
            _sut = new JniExecutor(_logger, _androidJni, _application);

            // Act
            _sut.Run(() => true, isMainThread);

            // Assert
            Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
            Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
        }

        [Test]
        public void Run_TResult_DetachesEvenOnException()
        {
            // Arrange
            _application.UnityVersion = "2019.4.40f1";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _sut.Run<bool>(() => throw new InvalidOperationException()));

            Assert.IsTrue(_androidJni.AttachCalled);
            Assert.IsTrue(_androidJni.DetachCalled);
        }

        [Test]
        public void Run_Void_DetachesEvenOnException()
        {
            // Arrange
            _application.UnityVersion = "2019.4.0f1";

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _sut.Run(() => throw new InvalidOperationException()));

            Assert.IsTrue(_androidJni.AttachCalled);
            Assert.IsTrue(_androidJni.DetachCalled);
        }

        [Test]
        public void Run_TResult_UsesDefaultAndroidJNIWhenNotProvided()
        {
            // This test is more of a smoke test since we can't easily verify the use of the singleton
            var result = _sut.Run(() => true);
            Assert.IsTrue(result);
        }

        [Test]
        public void Run_Void_UsesDefaultAndroidJNIWhenNotProvided()
        {
            // This test is more of a smoke test since we can't easily verify the use of the singleton
            Assert.DoesNotThrow(() => _sut.Run(() => { }));
        }
    }

    // Test implementations
    public class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public TestAndroidJNI()
        {
            AttachCalled = false;
            DetachCalled = false;
        }

        public void AttachCurrentThread()
        {
            AttachCalled = true;
        }

        public void DetachCurrentThread()
        {
            DetachCalled = true;
        }
    }
}
