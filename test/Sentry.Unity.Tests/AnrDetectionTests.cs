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
        private const int timeout = 500;
        private IDiagnosticLogger _logger = new TestLogger(forwardToUnityLog: true);
        private GameObject _gameObject = null!;
        private SentryMonoBehaviour _monoBehaviour = null!;
        private ANRWatchDog sut = null!;


        [SetUp]
        public void SetUp()
        {
            _gameObject ??= new GameObject("Tests");
            _monoBehaviour ??= _gameObject.AddComponent<SentryMonoBehaviour>();
        }

        [TearDown]
        public void TearDown() => sut.Stop(wait: true);

        private ANRWatchDog CreateWatchDog(bool multiThreaded)
        {
            UnityEngine.Debug.Log($"Preparing ANR watchdog: timeout={timeout} multiThreaded={multiThreaded}");
            return multiThreaded
                ? new ANRWatchDogMultiThreaded(_logger, _monoBehaviour, timeout)
                : new ANRWatchDogSingleThreaded(_logger, _monoBehaviour, timeout);
        }

        // Needed for [UnityTest] - IEnumerator return value
        // https://docs.unity3d.com/Packages/com.unity.test-framework@2.0/manual/reference-tests-parameterized.html
        static bool[] multiThreadingTestValues = new bool[] { true, false };

        [UnityTest]
        public IEnumerator DetectsStuckUI([ValueSource(nameof(multiThreadingTestValues))] bool multiThreaded)
        {
            ApplicationNotResponding? arn = null;
            sut = CreateWatchDog(multiThreaded);
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeout * 2 && arn is null)
            {
                Thread.Sleep(10);
            }

            // We need to let the single-threaded watchdog populate `arn` after UI became responsive again.
            if (!multiThreaded)
            {
                watch.Restart();
                while (watch.ElapsedMilliseconds < timeout && arn is null)
                {
                    yield return null;
                }
            }

            Assert.IsNotNull(arn);
            Assert.That(arn!.Message, Does.StartWith("Application not responding "));
        }

        [UnityTest]
        public IEnumerator DoesntReportWorkingUI([ValueSource(nameof(multiThreadingTestValues))] bool multiThreaded)
        {
            ApplicationNotResponding? arn = null;
            sut = CreateWatchDog(multiThreaded);
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // yield WaitForSeconds doesn't block the UI thread
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeout * 3)
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
            sut = CreateWatchDog(multiThreaded);
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            Thread.Sleep(timeout / 2);

            Assert.IsNull(arn);
        }
    }
}
