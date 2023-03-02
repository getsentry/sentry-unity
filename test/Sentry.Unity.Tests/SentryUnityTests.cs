using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Sentry.Extensibility;
using Sentry.Unity.Tests.SharedClasses;
using UnityEditor.PackageManager;

namespace Sentry.Unity.Tests
{
    public class SentryUnitySelfInitializationTests
    {
        [TearDown]
        public void TearDown()
        {
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.AddBreadcrumb("Closing the SDK for: " + TestContext.CurrentContext.Test.Name);
                SentryUnity.Close();
            }
        }

        [Test]
        public void SentryUnity_OptionsValid_Initializes()
        {
            var options = new SentryUnityOptions
            {
                Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880",
                Debug = true,
                DiagnosticLevel = SentryLevel.Debug
            };

            SentryUnity.Init(options);
            SentrySdk.AddBreadcrumb("Running: " + TestContext.CurrentContext.Test.Name);

            Assert.IsTrue(SentrySdk.IsEnabled);
        }

        [Test]
        public void SentryUnity_OptionsInvalid_DoesNotInitialize()
        {
            var options = new SentryUnityOptions
            {
                Debug = true,
                DiagnosticLevel = SentryLevel.Debug
            };

            // Even tho the defaults are set the DSN is missing making the options invalid for initialization
            SentryUnity.Init(options);
            SentrySdk.AddBreadcrumb("Running: " + TestContext.CurrentContext.Test.Name);

            Assert.IsFalse(SentrySdk.IsEnabled);
        }

        [Test]
        public void Init_MultipleTimes_LogsWarning()
        {
            var testLogger = new TestLogger(true);
            var options = new SentryUnityOptions
            {
                Debug = true,
                Dsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880",
                DiagnosticLogger = testLogger,
                DiagnosticLevel = SentryLevel.Debug
            };

            SentryUnity.Init(options);
            SentrySdk.AddBreadcrumb("Running: " + TestContext.CurrentContext.Test.Name);
            SentryUnity.Init(options);
            SentrySdk.AddBreadcrumb("Running: " + TestContext.CurrentContext.Test.Name);

            Assert.IsTrue(testLogger.Logs.Any(log =>
                log.logLevel == SentryLevel.Warning &&
                log.message.Contains("The SDK has already been initialized.")));
        }
    }
}
