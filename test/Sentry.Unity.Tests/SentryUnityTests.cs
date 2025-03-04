using NUnit.Framework;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Debug = UnityEngine.Debug;
using Sentry.Extensibility;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Tests;

public class SentryUnitySelfInitializationTests
{
    private const string TestDsn = "https://e9ee299dbf554dfd930bc5f3c90d5d4b@o447951.ingest.sentry.io/4504604988538880";

    [TearDown]
    public void TearDown()
    {
        if (SentrySdk.IsEnabled)
        {
            SentryUnity.Close();
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
            Dsn = TestDsn
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

    [Test]
    public void Init_MultipleTimes_LogsWarning()
    {
        var testLogger = new TestLogger();
        var options = new SentryUnityOptions
        {
            Debug = true,
            Dsn = TestDsn,
            DiagnosticLogger = testLogger,
        };

        SentryUnity.Init(options);
        SentryUnity.Init(options);

        Assert.IsTrue(testLogger.Logs.Any(log =>
            log.logLevel == SentryLevel.Warning &&
            log.message.Contains("The SDK has already been initialized.")));
    }

    [Test]
    public void GetLastRunState_WithoutInit_ReturnsUnknown()
    {
        // Make sure SDK is closed
        SentryUnity.Close();

        // Act
        var result = SentryUnity.GetLastRunState();

        // Assert
        Assert.AreEqual(SentryUnity.CrashedLastRun.Unknown, result);
    }

    [Test]
    public void GetLastRunState_WhenCrashed_ReturnsCrashed()
    {
        // Arrange
        var options = new SentryUnityOptions
        {
            Dsn = TestDsn,
            CrashedLastRun = () => true // Mock crashed state
        };

        // Act
        SentryUnity.Init(options);
        var result = SentryUnity.GetLastRunState();

        // Assert
        Assert.AreEqual(SentryUnity.CrashedLastRun.Crashed, result);
    }

    [Test]
    public void GetLastRunState_WhenNotCrashed_ReturnsDidNotCrash()
    {
        // Arrange
        var options = new SentryUnityOptions
        {
            Dsn = TestDsn,
            CrashedLastRun = () => false // Mock non-crashed state
        };

        // Act
        SentryUnity.Init(options);
        var result = SentryUnity.GetLastRunState();

        // Assert
        Assert.AreEqual(SentryUnity.CrashedLastRun.DidNotCrash, result);
    }

    [Test]
    public void GetLastRunState_WithNullDelegate_ReturnsUnknown()
    {
        // Arrange
        var options = new SentryUnityOptions
        {
            Dsn = TestDsn,
            CrashedLastRun = null // Explicitly set to null
        };

        // Act
        SentryUnity.Init(options);
        var result = SentryUnity.GetLastRunState();

        // Assert
        Assert.AreEqual(SentryUnity.CrashedLastRun.Unknown, result);
    }
}
