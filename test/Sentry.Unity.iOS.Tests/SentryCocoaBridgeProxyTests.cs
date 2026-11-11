using System.Collections.Generic;
using NUnit.Framework;

namespace Sentry.Unity.iOS.Tests;

public class SentryCocoaBridgeProxyTests
{
    private static Dictionary<string, object> WriteOptions(SentryUnityOptions options)
    {
        var written = new Dictionary<string, object>();
        SentryCocoaBridgeProxy.WriteOptions(options,
            (name, value) => written[name] = value,
            (name, value) => written[name] = value,
            (name, value) => written[name] = value,
            (_, _) => { });
        return written;
    }

    [Test]
    public void WriteOptions_SampleRateSet_SetsSampleRate()
    {
        var written = WriteOptions(new SentryUnityOptions { Dsn = "https://key@sentry.io/1", SampleRate = 0.25f });

        Assert.IsTrue(written.ContainsKey("sampleRate"));
        Assert.AreEqual(0.25, (double)written["sampleRate"]);
    }

    [Test]
    public void WriteOptions_SampleRateNotSet_LeavesSampleRateOut()
    {
        var written = WriteOptions(new SentryUnityOptions { Dsn = "https://key@sentry.io/1", SampleRate = null });

        Assert.IsFalse(written.ContainsKey("sampleRate"));
    }
}
