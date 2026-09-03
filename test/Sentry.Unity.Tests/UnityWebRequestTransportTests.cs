using NUnit.Framework;
using Sentry.Protocol.Envelopes;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

[TestFixture]
public class UnityWebRequestTransportTests
{
    private static SentryUnityOptions CreateOptions() => new()
    {
        Dsn = SentryTests.TestDsn
    };

    private static Envelope CreateEnvelope() => Envelope.FromEvent(new SentryEvent());

    /// <summary>
    /// The coroutine has to bail before its first yield. Anything else means a
    /// <see cref="UnityEngine.Networking.UnityWebRequest"/> got created, which is what raises the
    /// system connection dialog on platforms like the Nintendo Switch.
    /// </summary>
    [Test]
    public void SendEnvelopeAsync_NetworkNotReachable_DoesNotOpenAConnection()
    {
        var application = new TestApplication { InternetReachability = NetworkReachability.NotReachable };
        var transport = new UnityWebRequestTransport(CreateOptions(), application);

        var enumerator = transport.SendEnvelopeAsync(CreateEnvelope());

        Assert.IsFalse(enumerator.MoveNext(), "The transport attempted to send while offline.");
    }

    [Test]
    public void SendEnvelopeAsync_NetworkReachable_AttemptsToSend()
    {
        var application = new TestApplication
        {
            InternetReachability = NetworkReachability.ReachableViaLocalAreaNetwork
        };
        var transport = new UnityWebRequestTransport(CreateOptions(), application);

        var enumerator = transport.SendEnvelopeAsync(CreateEnvelope());

        Assert.IsTrue(enumerator.MoveNext(), "The transport did not attempt to send while online.");
    }
}
