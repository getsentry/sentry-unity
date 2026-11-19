using System;
using Sentry.Internal;

namespace Sentry.Unity.iOS;

/// <summary>
/// The replay that sentry-cocoa records, for the events captured in C#.
/// </summary>
internal sealed class CocoaReplaySession : IReplaySession
{
    private readonly ICocoaReplay _replay;

    public CocoaReplaySession(ICocoaReplay replay) => _replay = replay;

    // The replay id on the native scope, not the recording's: a replay buffered for errors only gets one once an
    // error captured it.
    public SentryId? ActiveReplayId => ToReplayId(_replay.GetReplayId());

    public SentryId? CaptureReplay(SentryEvent @event) => _replay.Capture() ? ToReplayId(_replay.GetReplayId()) : null;

    private static SentryId? ToReplayId(string? replayId)
    {
        if (replayId is null || !Guid.TryParse(replayId, out var guid) || guid == Guid.Empty)
        {
            return null;
        }

        return new SentryId(guid);
    }
}
