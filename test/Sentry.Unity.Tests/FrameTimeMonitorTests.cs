using NUnit.Framework;
using Sentry.Unity.Metrics;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class FrameTimeMonitorTests
{
    [TestCase(120, 0)]
    [TestCase(-1, 2)]
    public void Sample_EmitsRawFrameRateConfigurationAttributes(int targetFps, int vsyncCount)
    {
        var originalTargetFrameRate = Application.targetFrameRate;
        var originalVsyncCount = QualitySettings.vSyncCount;
        SentryMetric? capturedMetric = null;

        try
        {
            Application.targetFrameRate = targetFps;
            QualitySettings.vSyncCount = vsyncCount;

            var options = new SentryUnityOptions(application: new TestApplication())
            {
                Dsn = SentryTests.TestDsn
            };
            options.SetBeforeSendMetric(metric =>
            {
                capturedMetric ??= metric;
                return null;
            });
            SentrySdk.Init(options);

            new FrameTimeMonitor(options).Sample();

            Assert.That(capturedMetric, Is.Not.Null);
            Assert.That(capturedMetric!.Name, Is.EqualTo(FrameTimeMonitor.FrameTimeMetric));
            Assert.That(capturedMetric.TryGetAttribute(FrameTimeMonitor.TargetFpsAttribute, out int capturedTargetFps), Is.True);
            Assert.That(capturedTargetFps, Is.EqualTo(targetFps));
            Assert.That(capturedMetric.TryGetAttribute(FrameTimeMonitor.VsyncCountAttribute, out int capturedVsyncCount), Is.True);
            Assert.That(capturedVsyncCount, Is.EqualTo(vsyncCount));
        }
        finally
        {
            SentrySdk.Close();
            Application.targetFrameRate = originalTargetFrameRate;
            QualitySettings.vSyncCount = originalVsyncCount;
        }
    }
}
