using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Android.Tests
{
    [TestFixture]
    public class JniExecutorTests
    {
        private TestLogger _logger = null!; // Set during SetUp
        private JniExecutor _sut = null!; // Set during SetUp

        [SetUp]
        public void SetUp()
        {
            _logger = new TestLogger();
            _sut = new JniExecutor(_logger);
        }

        [TearDown]
        public void TearDown()
        {
            _sut.Dispose();
        }

        [Test]
        public void Run_Action_ExecutesSuccessfully()
        {
            // Arrange
            var executed = false;
            var action = () => executed = true;

            // Act
            _sut.Run(action);

            // Assert
            Assert.That(executed, Is.True);
        }

        [Test]
        public void Run_FuncBool_ReturnsExpectedResult()
        {
            // Arrange
            var func = () => true;

            // Act
            var result = _sut.Run(func);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void Run_FuncString_ReturnsExpectedResult()
        {
            // Arrange
            const string? expected = "Hello";
            var func = () => expected;

            // Act
            var result = _sut.Run(func);

            // Assert
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void Run_WithTimeout_LogsErrorOnTimeout()
        {
            // Arrange
            var slowAction = () => Thread.Sleep(100);
            var timeout = TimeSpan.FromMilliseconds(50);

            // Act
            _sut.Run(slowAction, timeout);

            // Assert
            Assert.IsTrue(_logger.Logs.Any(log =>
                log.logLevel == SentryLevel.Error &&
                log.message.Contains("JNI execution timed out.")));
        }

        [Test]
        public void Run_ThrowingOperation_LogsError()
        {
            // Arrange
            var exception = new Exception("Test exception");
            Action throwingAction = () => throw exception;

            // Act
            _sut.Run(throwingAction);

            // Assert
            Assert.IsTrue(_logger.Logs.Any(log =>
                log.logLevel == SentryLevel.Error &&
                log.message.Contains("Error during JNI execution.")));
        }

        [Test]
        public void Run_Generic_ReturnsDefaultOnException()
        {
            // Arrange
            Func<string> throwingFunc = () => throw new Exception("Test exception");

            // Act
            var result = _sut.Run(throwingFunc);

            // Assert
            Assert.That(result, Is.Null);
        }
    }
}
