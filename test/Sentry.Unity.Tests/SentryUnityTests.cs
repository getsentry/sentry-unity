using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Sentry.Extensibility;

namespace Sentry.Unity.Tests
{
    public class SentryUnitySelfInitializationTests : DisabledSelfInitializationTests
    {
        [TearDown]
        public void TearDown()
        {
            if (SentrySdk.IsEnabled)
            {
                SentrySdk.Close();
            }
        }

        [Test]
        public void AsyncStackTrace()
        {
            var options = new SentryUnityOptions();
            options.AttachStacktrace = true;
            var sut = new SentryStackTraceFactory(options);

            IList<SentryStackFrame> framesSentry = null!;
            StackFrame[] framesManual = null!;
            Task.Run(() =>
            {
                var stackTrace = new StackTrace(true);
                framesManual = stackTrace.GetFrames();

                var sentryStackTrace = sut.Create()!;
                var framesReversed = new System.Collections.Generic.List<SentryStackFrame>(sentryStackTrace.Frames);
                framesReversed.Reverse();
                framesSentry = framesReversed;
                return 42; // returning a value here messes up a stack trace
            }).Wait();

            Debug.Log("Manually captured stack trace:");
            foreach (var frame in framesManual)
            {
                Debug.Log($"  {frame} in {frame.GetMethod()?.DeclaringType?.FullName}");
            }

            Debug.Log("");

            Debug.Log("Sentry captured stack trace:");
            foreach (var frame in framesSentry)
            {
                Debug.Log($"  {frame.Function} in {frame.Module} from ({frame.Package})");
            }

            // Sentry captured frame must be cleaned up - the return type removed from the module (method name)
            Assert.AreEqual("System.Threading.Tasks.Task`1", framesSentry[0].Module);

            // Sanity check - the manually captured stack frame must contain the wrong format method
            Assert.IsTrue(framesManual[1].GetMethod()?.DeclaringType?.FullName?.StartsWith("System.Threading.Tasks.Task`1[[System.Int32"));
        }

        [Test]
        public void SentryUnity_OptionsValid_Initializes()
        {
            var options = new SentryUnityOptions
            {
                Dsn = "https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417"
            };

            SentryUnity.Init(options);

            Assert.IsTrue(SentrySdk.IsEnabled);
        }

        [Test]
        public void SentryUnity_OptionsInvalid_DoesNotInitialize()
        {
            var options = new SentryUnityOptions();

            // Even tho the defaults are set the DSN is missing making the options invalid for initialization
            SentryUnity.Init(options);

            Assert.IsFalse(SentrySdk.IsEnabled);
        }
    }
}
