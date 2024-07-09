using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Unity.Tests.Stubs;

internal sealed class TestHub : IHub
{
    private readonly List<SentryEvent> _capturedEvents = new();
    private readonly List<Action<Scope>> _configureScopeCalls = new();

    public IReadOnlyList<SentryEvent> CapturedEvents => _capturedEvents;
    public IReadOnlyList<Action<Scope>> ConfigureScopeCalls => _configureScopeCalls;

    public TestHub(bool isEnabled = true)
    {
        IsEnabled = isEnabled;
        Metrics = null!; // TODO: Don't do it like that
    }
    public bool IsEnabled { get; }

    public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, SentryHint? hint = null)
    {
        _capturedEvents.Add(evt);
        return evt.EventId;
    }

    public void CaptureUserFeedback(UserFeedback userFeedback)
    {
        throw new NotImplementedException();
    }

    public void CaptureTransaction(SentryTransaction transaction)
    {
    }

    public void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint)
    {
    }

    public void CaptureTransaction(SentryTransaction transaction, SentryHint? hint)
    {
        throw new NotImplementedException();
    }

    public void CaptureSession(SessionUpdate sessionUpdate)
    {
    }

    public SentryId CaptureCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null, TimeSpan? duration = null,
        Scope? scope = null, Action<SentryMonitorOptions>? configureMonitorOptions = null)
    {
        throw new NotImplementedException();
    }

    public SentryId CaptureCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null, TimeSpan? duration = null,
        Scope? scope = null)
    {
        throw new NotImplementedException();
    }

    public SentryId CaptureCheckIn(string monitorSlug, CheckInStatus status, SentryId? sentryId = null)
    {
        throw new NotImplementedException();
    }

    public bool CaptureEnvelope(Envelope envelope)
    {
        throw new NotImplementedException();
    }

    public Task FlushAsync(TimeSpan timeout)
    {
        return Task.CompletedTask;
    }

    public void ConfigureScope(Action<Scope> configureScope) => _configureScopeCalls.Add(configureScope);

    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;

    public void BindClient(ISentryClient client)
    {
        throw new NotImplementedException();
    }

    public IDisposable PushScope()
    {
        throw new NotImplementedException();
    }

    public IDisposable PushScope<TState>(TState state)
    {
        throw new NotImplementedException();
    }

    public void WithScope(Action<Scope> scopeCallback)
    {
        throw new NotImplementedException();
    }

    public SentryId LastEventId { get; }
    public IMetricAggregator Metrics { get; }

    public ITransactionTracer StartTransaction(ITransactionContext context, IReadOnlyDictionary<string, object?> customSamplingContext)
    {
        throw new NotImplementedException();
    }

    public void BindException(Exception exception, ISpan span)
    {
        throw new NotImplementedException();
    }

    ISpan? IHub.GetSpan()
    {
        throw new NotImplementedException();
    }

    public SentryTraceHeader? GetTraceHeader()
    {
        throw new NotImplementedException();
    }

    public BaggageHeader? GetBaggage()
    {
        throw new NotImplementedException();
    }

    public TransactionContext ContinueTrace(string? traceHeader, string? baggageHeader, string? name = null,
        string? operation = null)
    {
        throw new NotImplementedException();
    }

    public TransactionContext ContinueTrace(SentryTraceHeader? traceHeader, BaggageHeader? baggageHeader, string? name = null,
        string? operation = null)
    {
        throw new NotImplementedException();
    }

    public void StartSession()
    {
        // TODO: test sessions
    }

    public void PauseSession()
    {
        // TODO: test sessions
    }

    public void ResumeSession()
    {
        // TODO: test sessions
    }

    public void EndSession(SessionEndStatus status = SessionEndStatus.Exited)
    {
        // TODO: test sessions
    }

    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
    {
        throw new NotImplementedException();
    }

    public SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Action<Scope> configureScope)
    {
        throw new NotImplementedException();
    }
}