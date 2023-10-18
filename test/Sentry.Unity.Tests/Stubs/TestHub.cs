using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sentry.Unity.Tests.Stubs
{
    internal sealed class TestHub : IHub
    {
        private readonly List<SentryEvent> _capturedEvents = new();
        private readonly List<Action<Scope>> _configureScopeCalls = new();

        public IReadOnlyList<SentryEvent> CapturedEvents => _capturedEvents;
        public IReadOnlyList<Action<Scope>> ConfigureScopeCalls => _configureScopeCalls;

        public TestHub(bool isEnabled = true)
        {
            IsEnabled = isEnabled;
        }
        public bool IsEnabled { get; }

        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null)
        {
            _capturedEvents.Add(evt);
            return evt.EventId;
        }

        public SentryId CaptureEvent(SentryEvent evt, Hint? hint, Scope? scope = null)
        {
            throw new NotImplementedException();
        }

        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            throw new NotImplementedException();
        }

        public void CaptureTransaction(Transaction transaction)
        {
        }

        public void CaptureTransaction(Transaction transaction, Hint? hint)
        {
            throw new NotImplementedException();
        }

        public void CaptureSession(SessionUpdate sessionUpdate)
        {
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
        public ITransaction StartTransaction(ITransactionContext context, IReadOnlyDictionary<string, object?> customSamplingContext)
        {
            throw new NotImplementedException();
        }

        public void BindException(Exception exception, ISpan span)
        {
            throw new NotImplementedException();
        }

        public ISpan? GetSpan()
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
    }
}
