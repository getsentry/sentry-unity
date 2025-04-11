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
            var action = () =>
            {
                Thread.Sleep(100); // Simulate long-running operation
                executed = true;
                completionSource.SetResult(true);
            };

            // Act
            var stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
            _sut.RunAsync(action);
            stopwatch.Stop();

            // Assert
            Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(50), "RunAsync should return immediately");
            Assert.That(executed, Is.False, "Operation should not have completed yet");

            // Wait for async operation to complete
            Assert.That(completionSource.Task.Wait(500), Is.True, "Operation should eventually complete");
            Assert.That(executed, Is.True, "Operation should have completed after waiting");
        }

        [Test]
        public void RunAsync_MultipleOperations_AllExecuteEventually()
        {
            // Arrange
            var counter = 0;
            var manualReset = new ManualResetEventSlim();

            // Act
            for (int i = 0; i < 5; i++)
            {
                _sut.RunAsync(() => Interlocked.Increment(ref counter));
            }

            // Wait a bit to allow operations to execute
            Thread.Sleep(200);

            // Assert
            Assert.That(counter, Is.EqualTo(5), "All operations should be executed");
        }
    }
}
