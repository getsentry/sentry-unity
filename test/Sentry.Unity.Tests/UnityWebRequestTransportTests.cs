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
    /// A coroutine that yields has created a <see cref="UnityEngine.Networking.UnityWebRequest"/>,
    /// which is what raises the system connection dialog on platforms like the Nintendo Switch.
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

    /// <summary>
    /// The probe has the final say: on Nintendo Switch reachability claims the console is reachable
    /// while it is offline, which is the state the dialog gets raised in.
    /// </summary>
    [Test]
    public void SendEnvelopeAsync_ProbeReportsNoNetwork_OverridesReachability()
    {
        var application = new TestApplication
        {
            InternetReachability = NetworkReachability.ReachableViaLocalAreaNetwork
        };
        var options = CreateOptions();
        options.NetworkAvailabilityProbe = () => false;
        var transport = new UnityWebRequestTransport(options, application);

        var enumerator = transport.SendEnvelopeAsync(CreateEnvelope());

        Assert.IsFalse(enumerator.MoveNext(), "The transport ignored the platform's network probe.");
    }

    [Test]
    public void SendEnvelopeAsync_ProbeReportsNetwork_OverridesReachability()
    {
        var application = new TestApplication { InternetReachability = NetworkReachability.NotReachable };
        var options = CreateOptions();
        options.NetworkAvailabilityProbe = () => true;
        var transport = new UnityWebRequestTransport(options, application);

        var enumerator = transport.SendEnvelopeAsync(CreateEnvelope());

        Assert.IsTrue(enumerator.MoveNext(), "The transport ignored the platform's network probe.");
    }
}
