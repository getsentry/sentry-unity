using System;
using Sentry.Internal;

namespace Sentry.Unity.Android;

/// <summary>
/// The replay that the Android SDK records, for the events captured in C#.
/// </summary>
internal sealed class AndroidReplaySession : IReplaySession
{
    private readonly ISentryJava _sentryJava;

    public AndroidReplaySession(ISentryJava sentryJava) => _sentryJava = sentryJava;

    // The replay id on the native scope, not the recording's: a replay buffered for errors only gets one once an
    // error captured it.
    public SentryId? ActiveReplayId => ToReplayId(_sentryJava.GetReplayId());

    public SentryId? CaptureReplay(SentryEvent @event) => ToReplayId(_sentryJava.CaptureReplay());

    // The native SDK's empty id (all zeros) means no replay.
    private static SentryId? ToReplayId(string? replayId)
    {
        if (replayId is null || !Guid.TryParse(replayId, out var guid) || guid == Guid.Empty)
        {
            return null;
        }

        return new SentryId(guid);
    }
}
