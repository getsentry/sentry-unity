using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Unity;

/// <summary>
/// The trace of a native SDK that the app embedding the game owns, e.g. when using Unity as a Library.
/// The game's transactions join it, and events, logs, and outgoing requests outside of them continue it. Read on
/// every use, so the game follows when the app starts a new trace.
/// </summary>
internal sealed class NativePropagationContext : IExternalPropagationContext
{
    private readonly SentryOptions _options;
    private readonly Func<string?> _getTraceHeader;
    private readonly Func<string?> _getBaggageHeader;
    private readonly Func<ISpan?> _getSpan;
    private readonly SentryTraceHeader? _traceHeader;

    private NativePropagationContext(SentryOptions options, Func<string?> getTraceHeader,
        Func<string?> getBaggageHeader, Func<ISpan?> getSpan, SentryTraceHeader? traceHeader)
    {
        _options = options;
        _getTraceHeader = getTraceHeader;
        _getBaggageHeader = getBaggageHeader;
        _getSpan = getSpan;
        _traceHeader = traceHeader;
    }

    internal static void Use(SentryOptions options, Func<string?> getTraceHeader, Func<string?> getBaggageHeader,
        Func<ISpan?>? getSpan = null) =>
        options.ExternalPropagationContext = new NativePropagationContext(options, getTraceHeader, getBaggageHeader,
            getSpan ?? Sentry.SentrySdk.GetSpan, null);

    internal static ITransactionTracer StartTransaction(IHub hub, string name, string operation) =>
        hub.GetSentryOptions()?.ExternalPropagationContext is NativePropagationContext context
            ? context.StartTransactionInNativeTrace(hub, name, operation)
            : hub.StartTransaction(name, operation);

    // The game's own sampled transaction takes precedence. The native trace is the fallback.
    public IExternalPropagationContext Snapshot() =>
        new NativePropagationContext(_options, _getTraceHeader, _getBaggageHeader, _getSpan,
            _getSpan() is { IsSampled: true } ? null : Parse(_getTraceHeader()));

    // Child of the native SDK's active span, inheriting its sampling decision and DSC. Without one, the native trace
    // has no sampling decision, so the transaction is a root in it and the game decides.
    internal ITransactionTracer StartTransactionInNativeTrace(IHub hub, string name, string operation)
    {
        if (Parse(_getTraceHeader()) is not { } traceHeader)
        {
            return hub.StartTransaction(name, operation);
        }

        if (traceHeader.IsSampled is not { } isSampled)
        {
            return hub.StartTransaction(new TransactionContext(name, operation, traceId: traceHeader.TraceId));
        }

        var context = new TransactionContext(name, operation, parentSpanId: traceHeader.SpanId,
            traceId: traceHeader.TraceId, isSampled: isSampled, isParentSampled: isSampled);
        return hub.StartTransaction(context, new Dictionary<string, object?>(),
            GetNativeDynamicSamplingContext(traceHeader.TraceId));
    }

    public SentryId? TraceId => _traceHeader?.TraceId;
    public SpanId? SpanId => _traceHeader?.SpanId;
    public SpanId? ParentSpanId => null;
    public bool IsSampled => _traceHeader?.IsSampled ?? false;
    public double? SampleRate => null;
    public double? SampleRand => null;
    public DynamicSamplingContext? DynamicSamplingContext => null;

    public BaggageHeader GetBaggageHeader() =>
        GetDynamicSamplingContext(_options, null)?.ToBaggageHeader()
        ?? BaggageHeader.Create(new List<KeyValuePair<string, string>>());

    public DynamicSamplingContext? GetDynamicSamplingContext(SentryOptions options, IReplaySession? replaySession) =>
        this.CreateDynamicSamplingContext(options, replaySession);

    // Header and baggage are read separately, so the baggage is dropped if the native SDK moved to another trace.
    internal DynamicSamplingContext? GetNativeDynamicSamplingContext(SentryId traceId)
    {
        var baggageHeader = _getBaggageHeader();
        if (string.IsNullOrEmpty(baggageHeader)
            || BaggageHeader.TryParse(baggageHeader!, onlySentry: true)?.CreateDynamicSamplingContext() is not { } dsc)
        {
            return null;
        }

        return dsc.Items.TryGetValue("trace_id", out var dscTraceId)
               && string.Equals(dscTraceId, traceId.ToString(), StringComparison.OrdinalIgnoreCase)
            ? dsc
            : null;
    }

    private SentryTraceHeader? Parse(string? traceHeader)
    {
        if (string.IsNullOrEmpty(traceHeader))
        {
            return null;
        }

        try
        {
            return SentryTraceHeader.Parse(traceHeader!);
        }
        catch (Exception e)
        {
            _options.DiagnosticLogger?.LogWarning("Failed to parse the native trace header: {0}", e, traceHeader);
            return null;
        }
    }
}
