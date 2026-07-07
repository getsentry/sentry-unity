using System;
using UnityEngine.Scripting;

namespace Sentry.Unity;

/// <summary>
/// Forces IL2CPP to generate AOT code for the generic-virtual metric methods.
/// </summary>
/// <remarks>
/// The metric API dispatches through <c>SentryMetricEmitter.CaptureMetric&lt;T&gt;</c>, a generic
/// virtual method. IL2CPP cannot infer which value-type arguments reach that vtable slot, so it
/// transpiles no code for them and any call throws <c>ExecutionEngineException</c> at runtime.
/// Referencing every supported value type from a statically reachable call site makes IL2CPP emit
/// the instantiations ahead of time. This follows Unity's documented
/// <c>UsedOnlyForAOTCodeGeneration</c> pattern - the method is never executed.
/// </remarks>
[Preserve]
internal static class AotHints
{
    [Preserve]
    public static void UsedOnlyForAOTCodeGeneration()
    {
        var metrics = Sentry.SentrySdk.Metrics;

        metrics.EmitCounter("", (byte)0);
        metrics.EmitCounter("", (short)0);
        metrics.EmitCounter("", 0);
        metrics.EmitCounter("", 0L);
        metrics.EmitCounter("", 0f);
        metrics.EmitCounter("", 0d);

        metrics.EmitGauge("", (byte)0);
        metrics.EmitGauge("", (short)0);
        metrics.EmitGauge("", 0);
        metrics.EmitGauge("", 0L);
        metrics.EmitGauge("", 0f);
        metrics.EmitGauge("", 0d);

        metrics.EmitDistribution("", (byte)0);
        metrics.EmitDistribution("", (short)0);
        metrics.EmitDistribution("", 0);
        metrics.EmitDistribution("", 0L);
        metrics.EmitDistribution("", 0f);
        metrics.EmitDistribution("", 0d);

        throw new InvalidOperationException("This method is used for AOT code generation only. Do not call it at runtime.");
    }
}
