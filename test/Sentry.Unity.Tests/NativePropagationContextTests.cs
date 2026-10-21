using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests;

public class NativePropagationContextTests
{
    private const string TraceId = "75302ac48a024bde9a3b3734a82e36c8";
    private const string SpanId = "1000000000000000";

    private readonly TestLogger _logger = new();
    private string? _traceHeader;

    private SentryUnityOptions GetOptions()
    {
        var options = new SentryUnityOptions { Dsn = "https://key@sentry.io/1", DiagnosticLogger = _logger, Debug = true };
        NativePropagationContext.Use(options, () => _traceHeader);
        return options;
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
    public void GetBaggageHeader_NativeTraceHeader_ContainsNativeTraceId()
    {
        _traceHeader = $"{TraceId}-{SpanId}-1";

        var baggage = GetOptions().ExternalPropagationContext!.Snapshot().GetBaggageHeader();

        StringAssert.Contains($"sentry-trace_id={TraceId}", baggage.ToString());
    }
}
