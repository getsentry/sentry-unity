using System.Collections;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests
{
    public class AnrDetectionTests
    {
        private const int timeout = 500;
        private ANRWatchDog sut = null!;
        private GameObject _gameObject = null!;
        private SentryMonoBehaviour _monoBehaviour = null!;


        [SetUp]
        public void SetUp()
        {
            _gameObject ??= new GameObject("Tests");
            _monoBehaviour ??= _gameObject.AddComponent<SentryMonoBehaviour>();

            UnityEngine.Debug.Log($"Preparing ANR watchdog. Timeout: {timeout}");
            sut = new ANRWatchDog(timeout);
            sut.StartOnce(new TestLogger(forwardToUnityLog: true), _monoBehaviour);
        }

        [TearDown]
        public void TearDown() => sut.Stop(joinThread: true);

        [Test]
        public void DetectsStuckUI()
        {
            ApplicationNotResponding? arn = null;
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            var watch = Stopwatch.StartNew();
            while (watch.ElapsedMilliseconds < timeout * 2 && arn is null)
            {
                Thread.Sleep(10);
            }

            Assert.IsNotNull(arn);
            Assert.That(arn!.Message, Does.StartWith("Application not responding "));
        }

        [UnityTest]
        public IEnumerator DoesntReportWorkingUI()
        {
            ApplicationNotResponding? arn = null;
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
        public void DoesntReportShortlyStuckUI()
        {
            ApplicationNotResponding? arn = null;
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            Thread.Sleep(timeout / 2);

            Assert.IsNull(arn);
        }
    }
}
