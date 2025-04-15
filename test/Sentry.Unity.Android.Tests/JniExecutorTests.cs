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
        private JniExecutor _sut = null!;

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _androidJni = new TestAndroidJNI();
            _sut = new JniExecutor(_logger, _androidJni);
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
            var operationExecuted = false;
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
        [TestCase(true, false, Description = "main thread = should attach")]
        [TestCase(false, true, Description = "non main thread = should not attach")]
        public void Run_TResult_AttachesThreadBasedOnVersionAndThread(bool isMainThread, bool shouldAttach)
        {
            // Arrange
            _androidJni = new TestAndroidJNI();
            _sut = new JniExecutor(_logger, _androidJni);

            // Act
            _sut.Run(() => true, isMainThread);

            // Assert
            Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
            Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
        }

        [Test]
        [TestCase(true, false, Description = "main thread = should attach")]
        [TestCase(false, true, Description = "non main thread = should not attach")]
        public void Run_Void_AttachesThreadBasedOnVersionAndThread(bool isMainThread, bool shouldAttach)
        {
            // Arrange
            _androidJni = new TestAndroidJNI();
            _sut = new JniExecutor(_logger, _androidJni);

            // Act
            _sut.Run(() => true, isMainThread);

            // Assert
            Assert.AreEqual(shouldAttach, _androidJni.AttachCalled);
            Assert.AreEqual(shouldAttach, _androidJni.DetachCalled);
        }

        [Test]
        public void Run_TResult_DetachesEvenOnException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _sut.Run<bool>(() => throw new InvalidOperationException(), false));

            Assert.IsTrue(_androidJni.AttachCalled);
            Assert.IsTrue(_androidJni.DetachCalled);
        }

        [Test]
        public void Run_Void_DetachesEvenOnException()
        {
            // Act & Assert
            Assert.Throws<InvalidOperationException>(() =>
                _sut.Run(() => throw new InvalidOperationException(), false));

            Assert.IsTrue(_androidJni.AttachCalled);
            Assert.IsTrue(_androidJni.DetachCalled);
        }
    }

    public class TestAndroidJNI : IAndroidJNI
    {
        public bool AttachCalled { get; private set; }
        public bool DetachCalled { get; private set; }

        public void AttachCurrentThread() => AttachCalled = true;

        public void DetachCurrentThread() => DetachCalled = true;
    }
}
