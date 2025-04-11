using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
                log.message.Contains("JNI operation timed out after ")));
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
                log.message.Contains("Error during JNI operation execution.")));
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

        [Test]
        public void RunAsync_Action_DoesNotWaitForCompletion()
        {
            // Arrange
            var executed = false;
            var completionSource = new TaskCompletionSource<bool>();
            var fakeLongOperation = TimeSpan.FromMilliseconds(100);

            void Action()
            {
                Thread.Sleep(fakeLongOperation); // Simulate long-running operation
                executed = true;
                completionSource.SetResult(true);
            }

            // Act
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            _sut.RunAsync(Action);
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.Elapsed, Is.LessThan(fakeLongOperation), "RunAsync should return immediately");
            Assert.That(executed, Is.False, "Operation should not have completed yet");

            // Wait for fake long work to complete
            Assert.That(completionSource.Task.Wait(500), Is.True, "Operation should complete eventually");
            Assert.That(executed, Is.True, "Operation should have completed after waiting");
        }

        [Test]
        public void RunAsync_MultipleOperations_AllExecuteEventually()
        {
            // Arrange
            var counter = 0;
            const int expectedCount = 5;
            var countdownEvent = new CountdownEvent(expectedCount);

            // Act
            for (var i = 0; i < expectedCount; i++)
            {
                _sut.RunAsync(() => {
                    Interlocked.Increment(ref counter);
                    countdownEvent.Signal();
                });
            }

            // Wait with timeout instead of arbitrary sleep
            var allOperationsCompleted = countdownEvent.Wait(TimeSpan.FromMilliseconds(500));

            // Assert
            Assert.That(allOperationsCompleted, Is.True, "All operations should complete within the timeout");
            Assert.That(counter, Is.EqualTo(expectedCount));
        }
    }
}
