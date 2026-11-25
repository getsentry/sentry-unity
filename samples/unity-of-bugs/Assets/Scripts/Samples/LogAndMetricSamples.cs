namespace Sentry.Unity.Samples
{
    /// <summary>The structured log and metric samples.</summary>
    public static class LogAndMetricSamples
    {
        public static readonly SampleCategory Category = new SampleCategory("Logs & Metrics",
            new SampleEntry("logs/structured-logs", "Structured logs",
                "SentrySdk.Logger at info, warning and error.",
                SendLogs),
            new SampleEntry("logs/sensitive-log", "Log dropped by BeforeSendLog",
                "Logs a message starting with \"Sensitive:\", which BeforeSendLog drops: nothing should arrive.",
                SendSensitiveLog),
            new SampleEntry("logs/metrics", "Metrics",
                "SentrySdk.Metrics: a counter, a gauge and a distribution.",
                EmitMetrics));

        private static void SendLogs()
        {
            SentrySdk.Logger.LogInfo("The 🐛s awaken!");
            SentrySdk.Logger.LogWarning("Here come the bugs 🐞🦋🐛🐜🕷!");
            SentrySdk.Logger.LogError("A bug got away 🦋");
            SampleRunner.Report("Logs sent: info, warning and error");
        }

        private static void SendSensitiveLog()
        {
            // SentryOptionConfiguration's BeforeSendLog drops logs that start with "Sensitive:"
            SentrySdk.Logger.LogInfo("Sensitive: the player's home address");
            SampleRunner.Report("Dropped by BeforeSendLog: nothing should arrive");
        }

        private static void EmitMetrics()
        {
            SentrySdk.Metrics.EmitCounter("sample.bugs_squashed", 1);
            SentrySdk.Metrics.EmitGauge("sample.bugs_alive", 42, MeasurementUnit.None);
            SentrySdk.Metrics.EmitDistribution("sample.squash_time", 0.25, MeasurementUnit.Duration.Second);
            SampleRunner.Report("Metrics sent: counter, gauge and distribution");
        }
    }
}
