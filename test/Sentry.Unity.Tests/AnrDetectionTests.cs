using System.Collections;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Sentry.Extensibility;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests
{
    public class AnrDetectionTests
    {
        private const int Timeout = 500;
        private readonly IDiagnosticLogger _logger = new TestLogger(forwardToUnityLog: true);
        private GameObject _gameObject = null!;
        private SentryMonoBehaviour _monoBehaviour = null!;
        private AnrWatchDog _sut = null!;


        [SetUp]
        public void SetUp()
        {
            _gameObject = new GameObject("Tests");
            _monoBehaviour = _gameObject.AddComponent<SentryMonoBehaviour>();
        }

        [TearDown]
        public void TearDown() => _sut.Stop(wait: true);

        private AnrWatchDog CreateWatchDog(bool multiThreaded)
        {
            UnityEngine.Debug.Log($"Preparing ANR watchdog: timeout={Timeout} multiThreaded={multiThreaded}");
            return multiThreaded
                ? new AnrWatchDogMultiThreaded(_logger, _monoBehaviour, Timeout)
                : new AnrWatchDogSingleThreaded(_logger, _monoBehaviour, Timeout);
        }

        // Needed for [UnityTest] - IEnumerator return value
        // https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-tests-parameterized.html
        private static bool[] MultiThreadingTestValues = { true, false };

        [UnityTest]
        public IEnumerator DetectsStuckUI([ValueSource(nameof(MultiThreadingTestValues))] bool multiThreaded)
        {
            ApplicationNotResponding? arn = null;
            _sut = CreateWatchDog(multiThreaded);
            _sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < Timeout * 2 && arn is null)
            {
                Thread.Sleep(10);
            }

            // We need to let the single-threaded watchdog populate `arn` after UI became responsive again.
            if (!multiThreaded)
            {
                watch.Restart();
                while (watch.ElapsedMilliseconds < Timeout && arn is null)
                {
                    yield return null;
                }
            }

            Assert.IsNotNull(arn);
            Assert.That(arn!.Message, Does.StartWith("Application not responding "));
        }

        [UnityTest]
        public IEnumerator DoesntReportWorkingUI([ValueSource(nameof(MultiThreadingTestValues))] bool multiThreaded)
        {
            ApplicationNotResponding? arn = null;
            _sut = CreateWatchDog(multiThreaded);
            _sut.OnApplicationNotResponding += (_, e) => arn = e;

            // yield WaitForSeconds doesn't block the UI thread
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < Timeout * 3)
            {
                yield return new WaitForSeconds(0.01f);
            }

            Assert.IsNull(arn);
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void DoesntReportShortlyStuckUI(bool multiThreaded)
        {
            ApplicationNotResponding? arn = null;
            _sut = CreateWatchDog(multiThreaded);
            _sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            Thread.Sleep(Timeout / 2);

            Assert.IsNull(arn);
        }
    }
}
