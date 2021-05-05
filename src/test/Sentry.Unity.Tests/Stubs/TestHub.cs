using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sentry.Unity.Tests.Stubs
{
    internal sealed class TestHub : IHub
    {
        private readonly List<SentryEvent> _capturedEvents = new();

        public IReadOnlyList<SentryEvent> CapturedEvents => _capturedEvents;

        public bool IsEnabled { get; } = true;

        public SentryId CaptureEvent(SentryEvent evt, Scope? scope = null)
        {
            _capturedEvents.Add(evt);
            return evt.EventId;
        }

        public void CaptureUserFeedback(UserFeedback userFeedback)
        {
            throw new NotImplementedException();
        }

        public void CaptureTransaction(Transaction transaction)
        {
        }

        public Task FlushAsync(TimeSpan timeout)
        {
            throw new NotImplementedException();
        }

        public void ConfigureScope(Action<Scope> configureScope)
        {
            _ = configureScope;
        }

        public Task ConfigureScopeAsync(Func<Scope, Task> configureScope)
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
    }
}
