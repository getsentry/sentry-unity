using NUnit.Framework;
using UnityEngine.TestTools;

namespace Sentry.Unity.Tests
{
    public sealed class UnityLoggerTests
    {
        [Test]
        public void Log_DebugLevels_Correspond([Values] SentryLevel sentryLevel)
        {
            LogAssert.ignoreFailingMessages = true;

            var interceptor = new TestUnityLoggerInterceptor();
            var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = sentryLevel }, interceptor);

            const string expectedLog = "Some log";
            logger.Log(sentryLevel, expectedLog);

            Assert.True(logger.IsEnabled(sentryLevel));
            Assert.True(interceptor.LogMessage.Contains("Sentry"));
            Assert.True(interceptor.LogMessage.Contains(expectedLog));
            Assert.True(interceptor.LogMessage.Contains(sentryLevel.ToString()));
        }

        [TestCaseSource(nameof(SentryLevels))]
        public void Log_LowerLevelThanInitializationLevel_DisablesLogger(SentryLevel initializationLevel, SentryLevel lowerLevel)
        {
            var interceptor = new TestUnityLoggerInterceptor();
            var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = initializationLevel }, interceptor);

            const string expectedLog = "Some log";

            logger.Log(lowerLevel, expectedLog);

            Assert.False(logger.IsEnabled(lowerLevel));
            Assert.False(interceptor.LogMessage.Contains(expectedLog));
        }

        [Test]
        public void Log_StartsWithLogPrefix()
        {
            var interceptor = new TestUnityLoggerInterceptor();
            var logger = new UnityLogger(new SentryOptions { DiagnosticLevel = SentryLevel.Debug }, interceptor);

            logger.Log(SentryLevel.Debug, "TestLog");

            StringAssert.StartsWith(UnityLogger.LogPrefix, interceptor.LogMessage);
        }

        private static object[] SentryLevels =
        {
            new object[] { SentryLevel.Info, SentryLevel.Debug },
            new object[] { SentryLevel.Warning, SentryLevel.Info },
            new object[] { SentryLevel.Error, SentryLevel.Warning },
            new object[] { SentryLevel.Fatal, SentryLevel.Error }
        };

        private sealed class TestUnityLoggerInterceptor : IUnityLoggerInterceptor
        {
            public string LogMessage { get; private set; } = string.Empty;

            public void Intercept(SentryLevel level, string logMessage) => LogMessage = logMessage;
        }
    }
}
