using System;
using System.Collections.Generic;
using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry.Unity;

/// <summary>
/// The trace of a native SDK that the app embedding the game owns, e.g. when using Unity as a Library.
/// Events, logs, and outgoing requests from the game continue it. Read on every use, so the game follows when the app
/// starts a new trace.
/// </summary>
internal sealed class NativePropagationContext : IExternalPropagationContext
{
    private readonly SentryOptions _options;
    private readonly Func<string?> _getTraceHeader;
    private readonly SentryTraceHeader? _traceHeader;

    private NativePropagationContext(SentryOptions options, Func<string?> getTraceHeader, SentryTraceHeader? traceHeader)
    {
        _options = options;
        _getTraceHeader = getTraceHeader;
        _traceHeader = traceHeader;
    }

    internal static void Use(SentryOptions options, Func<string?> getTraceHeader) =>
        options.ExternalPropagationContext = new NativePropagationContext(options, getTraceHeader, null);

    public IExternalPropagationContext Snapshot() =>
        new NativePropagationContext(_options, _getTraceHeader, Parse(_getTraceHeader()));

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
