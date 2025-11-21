using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class SentryUnityOptionsExtensionsTests
{
    private class Fixture
    {
        public TestApplication TestApplication { get; set; } = new();
        public bool Enabled { get; set; } = true;
        public string Dsn { get; set; } = "http://test.com";
        public bool CaptureInEditor { get; set; } = true;
        public bool Debug { get; set; } = true;

        public SentryUnityOptions GetSut() => new()
        {
            Enabled = Enabled,
            Dsn = Dsn,
            CaptureInEditor = CaptureInEditor,
            Debug = Debug,
        };
    }

    private Fixture _fixture = new();

    [SetUp]
    public void SetUp() => _fixture = new Fixture();

    [Test]
    public void Validate_OptionsDisabled_ReturnsFalse()
    {
        _fixture.Enabled = false;
        var options = _fixture.GetSut();

        var isValid = options.IsValid();

        Assert.IsFalse(isValid);
    }

    [Test]
    public void Validate_DsnEmpty_ReturnsFalse()
    {
        _fixture.Dsn = string.Empty;
        var options = _fixture.GetSut();

        var isValid = options.IsValid();

        Assert.IsFalse(isValid);
    }

    [Test]
    public void ShouldInitializeSdk_CorrectlyConfiguredForEditor_ReturnsTrue()
    {
        var options = _fixture.GetSut();

        var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

        Assert.IsTrue(shouldInitialize);
    }

    [Test]
    public void ShouldInitializeSdk_OptionsNull_ReturnsFalse()
    {
        _fixture.TestApplication = new TestApplication(false);
        SentryUnityOptions? options = null;

        var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

        Assert.IsFalse(shouldInitialize);
    }

    [Test]
    public void ShouldInitializeSdk_CorrectlyConfigured_ReturnsTrue()
    {
        _fixture.TestApplication = new TestApplication(false);
        var options = _fixture.GetSut();

        var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

        Assert.IsTrue(shouldInitialize);
    }

    [Test]
    public void ShouldInitializeSdk_NotCaptureInEditorAndApplicationIsEditor_ReturnsFalse()
    {
        _fixture.CaptureInEditor = false;
        var options = _fixture.GetSut();

        var shouldInitialize = options.ShouldInitializeSdk(_fixture.TestApplication);

        Assert.IsFalse(shouldInitialize);
    }

    [Test]
    public void SetupLogging_DebugAndNoDiagnosticLogger_SetsUnityLogger()
    {
        var options = _fixture.GetSut();

        Assert.IsNull(options.DiagnosticLogger); // Sanity check

        options.SetupUnityLogging();

        Assert.IsInstanceOf<UnityLogger>(options.DiagnosticLogger);
    }

    [Test]
    public void SetupLogging_DebugFalse_DiagnosticLoggerIsNull()
    {
        _fixture.Debug = false;
        var options = _fixture.GetSut();
        options.DiagnosticLogger = new UnityLogger(options);

        options.SetupUnityLogging();

        Assert.IsNull(options.DiagnosticLogger);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public void SetupLogging_DiagnosticLoggerSet_LeavesOrRemovesDiagnosticLogger(bool debug)
    {
        _fixture.Debug = debug;
        var options = _fixture.GetSut();
        options.DiagnosticLogger = new UnityLogger(options);

        options.SetupUnityLogging();

        Assert.AreEqual(debug, options.DiagnosticLogger is not null);
    }

    [Test]
    public void DisableUnityLoggingIntegration_RemovesUnityApplicationLoggingIntegration()
    {
        var options = _fixture.GetSut();

        Assert.IsTrue(options.Integrations.Any(i => i is Integrations.UnityApplicationLoggingIntegration));

        options.DisableUnityLoggingIntegration();

        Assert.IsFalse(options.Integrations.Any(i => i is Integrations.UnityApplicationLoggingIntegration));
    }

    [Test]
    public void DisableUnhandledExceptionCapture_RemovesUnityLogHandlerIntegration()
    {
        var options = _fixture.GetSut();

        Assert.IsTrue(options.Integrations.Any(i => i is Integrations.UnityLogHandlerIntegration));

        options.DisableUnhandledExceptionCapture();

        Assert.IsFalse(options.Integrations.Any(i => i is Integrations.UnityLogHandlerIntegration));
    }

    [Test]
    public void DisableUnhandledExceptionCapture_RemovesUnityWebGLExceptionHandler()
    {
        var application = new TestApplication(isEditor: false, platform: UnityEngine.RuntimePlatform.WebGLPlayer);
        var options = new SentryUnityOptions(application: application)
        {
            Enabled = true,
            Dsn = "http://test.com",
            CaptureInEditor = true,
            Debug = true,
        };

        Assert.IsTrue(options.Integrations.Any(i => i is Integrations.UnityWebGLExceptionHandler));

        options.DisableUnhandledExceptionCapture();

        Assert.IsFalse(options.Integrations.Any(i => i is Integrations.UnityWebGLExceptionHandler));
    }

    [Test]
    public void DisableUnhandledExceptionCapture_DoesNotRemoveUnityApplicationLoggingIntegration()
    {
        var options = _fixture.GetSut();

        options.DisableUnhandledExceptionCapture();

        Assert.IsTrue(options.Integrations.Any(i => i is Integrations.UnityApplicationLoggingIntegration));
    }

    [Test]
    [TestCase(RuntimePlatform.PS4, true, true)]
    [TestCase(RuntimePlatform.PS4, false, false)]
    [TestCase(RuntimePlatform.PS5, true, true)]
    [TestCase(RuntimePlatform.PS5, false, false)]
    [TestCase(RuntimePlatform.GameCoreXboxSeries, true, true)]
    [TestCase(RuntimePlatform.GameCoreXboxSeries, false, false)]
    [TestCase(RuntimePlatform.GameCoreXboxOne, true, true)]
    [TestCase(RuntimePlatform.GameCoreXboxOne, false, false)]
    public void IsNativeSupportEnabled_ConsolePlatforms_ReturnsExpectedValue(
        RuntimePlatform platform, bool optionEnabled, bool expectedResult)
    {
        var options = _fixture.GetSut();
        options.PlayStationNativeSupportEnabled = platform is RuntimePlatform.PS4 or RuntimePlatform.PS5
            ? optionEnabled
            : options.PlayStationNativeSupportEnabled;
        options.XboxNativeSupportEnabled = platform is RuntimePlatform.GameCoreXboxSeries or RuntimePlatform.GameCoreXboxOne
            ? optionEnabled
            : options.XboxNativeSupportEnabled;

        var result = options.IsNativeSupportEnabled(platform);

        Assert.AreEqual(expectedResult, result);
    }
}
