using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    /// <summary>
    /// Testing debouncer in realtime.
    /// </summary>
    public sealed class DebouncerTests
    {
        private readonly TimeSpan DefaultOffset = TimeSpan.FromMilliseconds(100);

        [UnityTest]
        [Ignore("Ignored due to flakiness")] // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        public IEnumerator LogTimeDebounce()
        {
            yield return AssertDefaultDebounce(new LogTimeDebounce(DefaultOffset));
        }

        [UnityTest]
        [Ignore("Ignored due to flakiness")] // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        public IEnumerator ErrorTimeDebounce()
        {
            yield return AssertDefaultDebounce(new ErrorTimeDebounce(DefaultOffset));
        }

        [UnityTest]
        [Ignore("Ignored due to flakiness")] // Ignoring because of flakiness: https://github.com/getsentry/sentry-unity/issues/335
        public IEnumerator WarningTimeDebounce()
        {
            yield return AssertDefaultDebounce(new WarningTimeDebounce(DefaultOffset));
        }

        private IEnumerator AssertDefaultDebounce(TimeDebounceBase debouncer)
        {
            // pass
            Assert.IsTrue(debouncer.Debounced());

            yield return new WaitForSeconds(0.050f);

            // skip
            Assert.IsFalse(debouncer.Debounced());

            yield return new WaitForSeconds(0.02f);

            // skip
            Assert.IsFalse(debouncer.Debounced());

            yield return new WaitForSeconds(0.04f);

            // pass
            Assert.IsTrue(debouncer.Debounced());
        }
    }
}
