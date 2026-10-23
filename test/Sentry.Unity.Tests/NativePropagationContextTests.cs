using System.Linq;
using NUnit.Framework;
using Sentry.Internal;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Tests;

public class NativePropagationContextTests
{
    private const string TraceId = "75302ac48a024bde9a3b3734a82e36c8";
    private const string SpanId = "1000000000000000";

    private readonly TestLogger _logger = new();
    private string? _traceHeader;
    private string? _baggageHeader;
    private ISpan? _span;

    private SentryUnityOptions GetOptions()
    {
        var options = new SentryUnityOptions { Dsn = "https://key@sentry.io/1", DiagnosticLogger = _logger, Debug = true };
        NativePropagationContext.Use(options, () => _traceHeader, () => _baggageHeader, () => _span);
        return options;
    }

    private NativePropagationContext GetSut() => (NativePropagationContext)GetOptions().ExternalPropagationContext!;

    private static ISpan GetSpan(bool isSampled)
    {
        var context = new TransactionContext("name", "operation");
        return isSampled ? new TransactionTracer(new TestHub(), context) : new UnsampledTransaction(new TestHub(), context);
    }

    [Test]
    public void Snapshot_NativeTraceHeader_ContinuesNativeTrace()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.AreEqual(SentryId.Parse(TraceId), context.TraceId);
        Assert.AreEqual(Sentry.SpanId.Parse(SpanId), context.SpanId);
        Assert.IsTrue(context.IsSampled);
    }

    [Test]
    public void Snapshot_NoNativeTraceHeader_HasNoTrace()
    {
        _traceHeader = null;

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.IsNull(context.TraceId);
        Assert.IsNull(context.SpanId);
    }

    [Test]
    public void Snapshot_NativeTraceChanges_FollowsNativeTrace()
    {
        var options = GetOptions();
        _traceHeader = $"{SentryId.Create()}-{SpanId}";
        var first = options.ExternalPropagationContext!.Snapshot();

        _traceHeader = $"{TraceId}-{SpanId}";
        var second = options.ExternalPropagationContext!.Snapshot();

        Assert.AreNotEqual(first.TraceId, second.TraceId);
        Assert.AreEqual(SentryId.Parse(TraceId), second.TraceId);
    }

    [Test]
    public void Snapshot_InvalidNativeTraceHeader_LogsAndHasNoTrace()
    {
        _traceHeader = "invalid";

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.IsNull(context.TraceId);
        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Warning && log.message.Contains("Failed to parse the native trace header")));
    }

    [Test]
    public void Snapshot_SampledSpanActive_DefersToSpan()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";
        _span = GetSpan(isSampled: true);

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.IsNull(context.TraceId);
    }

    [Test]
    public void Snapshot_UnsampledSpanActive_ContinuesNativeTrace()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";
        _span = GetSpan(isSampled: false);

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.AreEqual(SentryId.Parse(TraceId), context.TraceId);
    }

    [Test]
    public void GetBaggageHeader_NativeTraceHeader_ContainsNativeTraceId()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";

        var baggage = GetOptions().ExternalPropagationContext!.Snapshot().GetBaggageHeader();

        StringAssert.Contains($"sentry-trace_id={TraceId}", baggage.ToString());
    }

    [Test]
    public void Snapshot_NativeBaggageOfSameTrace_UsesNativeDsc()
    {
        _traceHeader = $"{TraceId}-{SpanId}";
        _baggageHeader = $"sentry-trace_id={TraceId},sentry-public_key=native,sentry-sample_rate=0.5,sentry-sample_rand=0.25";

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        var dsc = context.GetDynamicSamplingContext(new SentryOptions(), null!);
        Assert.AreSame(context.DynamicSamplingContext, dsc);
        Assert.AreEqual("native", dsc!.Items["public_key"]);
        Assert.AreEqual("0.25", dsc.Items["sample_rand"]);
        var baggage = context.GetBaggageHeader().ToString();
        StringAssert.Contains("sentry-public_key=native", baggage);
        StringAssert.Contains("sentry-sample_rand=0.25", baggage);
        StringAssert.DoesNotContain("sentry-sampled", baggage);
    }

    [Test]
    public void Snapshot_NativeBaggageOfOtherTrace_FallsBackToOwnDsc()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";
        _baggageHeader = $"sentry-trace_id={SentryId.Create()},sentry-public_key=native,sentry-sample_rand=0.25";

        var context = GetOptions().ExternalPropagationContext!.Snapshot();

        Assert.IsNull(context.DynamicSamplingContext);
        var baggage = context.GetBaggageHeader().ToString();
        StringAssert.Contains($"sentry-trace_id={TraceId}", baggage);
        StringAssert.Contains("sentry-public_key=key", baggage);
        StringAssert.DoesNotContain("sentry-sample_rand=0.25", baggage);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void StartTransactionInNativeTrace_NativeSpan_ChildInheritsSamplingDecision(bool isSampled)
    {
        _traceHeader = $"{TraceId}-{SpanId}-{(isSampled ? 1 : 0)}";
        var hub = new TestHub();

        GetSut().StartTransactionInNativeTrace(hub, "name", "operation");

        var context = (TransactionContext)hub.StartedTransactions.Single();
        Assert.AreEqual(SentryId.Parse(TraceId), context.TraceId);
        Assert.AreEqual(Sentry.SpanId.Parse(SpanId), context.ParentSpanId);
        Assert.AreEqual(isSampled, context.IsSampled);
        Assert.AreEqual(isSampled, context.IsParentSampled);
    }

    [Test]
    public void StartTransactionInNativeTrace_NoNativeSpan_RootWithDeferredSampling()
    {
        _traceHeader = $"{TraceId}-{SpanId}";
        var hub = new TestHub();

        GetSut().StartTransactionInNativeTrace(hub, "name", "operation");

        var context = (TransactionContext)hub.StartedTransactions.Single();
        Assert.AreEqual(SentryId.Parse(TraceId), context.TraceId);
        Assert.IsNull(context.ParentSpanId);
        Assert.IsNull(context.IsSampled);
        Assert.IsNull(context.IsParentSampled);
    }

    [Test]
    public void StartTransactionInNativeTrace_NoNativeTrace_StartsNewTrace()
    {
        _traceHeader = null;

        var transaction = GetSut().StartTransactionInNativeTrace(new TestHub(), "name", "operation");

        Assert.AreNotEqual(SentryId.Parse(TraceId), transaction.TraceId);
        Assert.IsNull(transaction.ParentSpanId);
    }

    [Test]
    public void GetNativeDynamicSamplingContext_BaggageOfSameTrace_ReturnsNativeDsc()
    {
        _baggageHeader = $"sentry-trace_id={TraceId},sentry-public_key=key,sentry-sample_rate=0.5,sentry-sample_rand=0.25,sentry-sampled=true";

        var dsc = GetSut().GetNativeDynamicSamplingContext(SentryId.Parse(TraceId));

        Assert.IsNotNull(dsc);
        Assert.AreEqual("0.25", dsc!.Items["sample_rand"]);
    }

    [Test]
    public void GetNativeDynamicSamplingContext_BaggageOfOtherTrace_ReturnsNull()
    {
        _baggageHeader = $"sentry-trace_id={SentryId.Create()},sentry-public_key=key,sentry-sample_rate=0.5,sentry-sampled=true";

        var dsc = GetSut().GetNativeDynamicSamplingContext(SentryId.Parse(TraceId));

        Assert.IsNull(dsc);
    }
}
