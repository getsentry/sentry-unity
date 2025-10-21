using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Sentry.Protocol.Envelopes;

namespace Sentry.Unity.Tests.Stubs;

internal sealed class TestHub : IHub
{
    private readonly List<SentryEvent> _capturedEvents = new();
    private readonly List<SentryTransaction> _capturedTransactions = new();
    private readonly List<Action<Scope>> _configureScopeCalls = new();

    public IReadOnlyList<SentryEvent> CapturedEvents => _capturedEvents;
    public IReadOnlyList<SentryTransaction> CapturedTransactions => _capturedTransactions;
    public IReadOnlyList<Action<Scope>> ConfigureScopeCalls => _configureScopeCalls;

    public TestHub(bool isEnabled = true)
    {
#pragma warning disable SENTRY0001
        Logger = null!;
#pragma warning restore SENTRY0001
        IsEnabled = isEnabled;
    }
    public bool IsEnabled { get; }

    public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null, SentryHint? hint = null)
    {
        _capturedEvents.Add(evt);
        return evt.EventId;
    }

    public void CaptureFeedback(SentryFeedback feedback, Scope? scope = null, SentryHint? hint = null)
    {
        throw new NotImplementedException();
    }

    public void CaptureTransaction(SentryTransaction transaction) =>
        _capturedTransactions.Add(transaction);

    public void CaptureTransaction(SentryTransaction transaction, Scope? scope, SentryHint? hint) =>
        _capturedTransactions.Add(transaction);

    public void CaptureTransaction(SentryTransaction transaction, SentryHint? hint) =>
        _capturedTransactions.Add(transaction);

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
    public void ConfigureScope<TArg>(Action<Scope, TArg> configureScope, TArg arg) =>
        ConfigureScope(scope => configureScope.Invoke(scope, arg));

    public Task ConfigureScopeAsync(Func<Scope, Task> configureScope) => Task.CompletedTask;
    public Task ConfigureScopeAsync<TArg>(Func<Scope, TArg, Task> configureScope, TArg arg) =>
        ConfigureScopeAsync(scope => configureScope.Invoke(scope, arg));

    public void SetTag(string key, string value)
    {
        throw new NotImplementedException();
    }

    public void UnsetTag(string key)
    {
        throw new NotImplementedException();
    }

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

#pragma warning disable SENTRY0001
    public SentryStructuredLogger Logger { get; }
#pragma warning restore SENTRY0001

    public ITransactionTracer StartTransaction(ITransactionContext context, IReadOnlyDictionary<string, object?> customSamplingContext) =>
        new TransactionTracer(this, context);

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

    public bool IsSessionActive { get; }

    public SentryId CaptureEvent(SentryEvent evt, Action<Scope> configureScope)
    {
        throw new NotImplementedException();
    }

    public SentryId CaptureEvent(SentryEvent evt, SentryHint? hint, Action<Scope> configureScope)
    {
        throw new NotImplementedException();
    }

    public void CaptureFeedback(SentryFeedback feedback, Action<Scope> configureScope, SentryHint? hint = null)
    {
        throw new NotImplementedException();
    }
}
