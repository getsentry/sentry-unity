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
        private const int timeout = 1000;

        private ANRWatchDog StartWatchdog()
        {
            var gameObject = new GameObject("Tests");
            var monoBehaviour = gameObject.AddComponent<SentryMonoBehaviour>();

            var sut = new ANRWatchDog(timeout);
            sut.StartOnce(new TestLogger(), monoBehaviour);
            return sut;
        }

        [UnityTest]
        public IEnumerator DetectsStuckUI()
        {
            yield return null;

            var sut = StartWatchdog();
            ApplicationNotResponding? arn = null;
            sut.OnApplicationNotResponding += (_, e) => arn = e;

            // Thread.Sleep blocks the UI thread
            Thread.Sleep(timeout * 2);

            Assert.IsNotNull(arn);
            Assert.AreEqual(arn!.Message, $"Application not responding for at least {timeout} ms.");
        }

        [UnityTest]
        public IEnumerator DoesntReportWorkingUI()
        {
            yield return null;

            var sut = StartWatchdog();
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
    }
}
