using System;
using System.Collections.Generic;

namespace Sentry.Unity.Tests.Stubs;

internal sealed class TestStructuredLogger : SentryStructuredLogger
{
    public List<(string level, string message, object[] args)> LogCalls { get; } = new();

    private protected override void CaptureLog(SentryLogLevel level, string template, object[]? parameters, Action<SentryLog>? configureLog)
        => LogCalls.Add((level.ToString(), template, parameters ?? []));

    protected internal override void CaptureLog(SentryLog log)
    {
        // Not needed for our tests
    }

    protected internal override void Flush()
    {
        // Not needed for our tests
    }

    public void Clear() => LogCalls.Clear();
}
