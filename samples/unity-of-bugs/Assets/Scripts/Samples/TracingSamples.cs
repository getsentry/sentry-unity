using System.Collections;
using UnityEngine;

namespace Sentry.Unity.Samples
{
    /// <summary>The tracing sample: a transaction with child spans.</summary>
    public static class TracingSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Tracing",
            new SampleEntry("tracing/transaction", "Transaction with spans",
                "Starts a transaction and two child spans, then finishes them.",
                RunTransaction));

        private static IEnumerator RunTransaction()
        {
            var transaction = SentrySdk.StartTransaction("Unity of Bugs sample", "sample.transaction");

            var load = transaction.StartChild("sample.load", "Load the level");
            yield return new WaitForSeconds(0.2f);
            load.Finish();

            var spawn = transaction.StartChild("sample.spawn", "Spawn the bugs");
            yield return new WaitForSeconds(0.3f);
            spawn.Finish();

            transaction.Finish();
            SampleRunner.Report($"Transaction sent: trace {transaction.TraceId}");
        }
    }
}
