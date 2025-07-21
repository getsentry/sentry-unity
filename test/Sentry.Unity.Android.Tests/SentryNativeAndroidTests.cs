using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;

namespace Sentry.Unity.Android.Tests;

public class SentryNativeAndroidTests
{
    private bool _reinstallCalled;
    private Action? _originalReinstallSentryNativeBackendStrategy;
    private Action _fakeReinstallSentryNativeBackendStrategy;
    private TestUnityInfo _sentryUnityInfo = null!;
    private TestSentryJava _testSentryJava = null!;
    private readonly TestLogger _logger = new();

    public SentryNativeAndroidTests()
        => _fakeReinstallSentryNativeBackendStrategy = () => _reinstallCalled = true;

    [SetUp]
    public void SetUp()
    {
        _originalReinstallSentryNativeBackendStrategy =
            Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy,
                _fakeReinstallSentryNativeBackendStrategy);
        _reinstallCalled = false;
        _sentryUnityInfo = new TestUnityInfo { IL2CPP = false };

        _testSentryJava = new TestSentryJava();
        SentryNativeAndroid.SentryJava = _testSentryJava;
    }

    [TearDown]
    public void TearDown() =>
        _fakeReinstallSentryNativeBackendStrategy =
            Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy!,
                _originalReinstallSentryNativeBackendStrategy)!;

    [Test]
    public void Configure_DefaultConfiguration_SetsScopeObserver()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);

        SentryNativeAndroid.Configure(options);

        Assert.IsAssignableFrom<AndroidJavaScopeObserver>(options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_SetsCrashedLastRun()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);

        SentryNativeAndroid.Configure(options);

        Assert.IsNotNull(options.CrashedLastRun);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);
        options.AndroidNativeSupportEnabled = false;

        SentryNativeAndroid.Configure(options);

        Assert.Null(options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_EnablesScopeSync()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);

        SentryNativeAndroid.Configure(options);

        Assert.True(options.EnableScopeSync);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);
        options.AndroidNativeSupportEnabled = false;

        SentryNativeAndroid.Configure(options);

        Assert.False(options.EnableScopeSync);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void Configure_IL2CPP_ReInitializesNativeBackend(bool il2cpp, bool expectedReinstall)
    {
        _sentryUnityInfo.IL2CPP = il2cpp;
        var options = new SentryUnityOptions(_sentryUnityInfo);

        Assert.False(_reinstallCalled); // Sanity check

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual(expectedReinstall, _reinstallCalled);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);
        options.AndroidNativeSupportEnabled = false;

        SentryNativeAndroid.Configure(options);

        Assert.False(_reinstallCalled);
    }

    [Test]
    public void Configure_NoInstallationIdReturned_SetsNewDefaultUserId()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo);
        _testSentryJava.InstallationId = string.Empty;

        SentryNativeAndroid.Configure(options);

        Assert.False(string.IsNullOrEmpty(options.DefaultUserId));
    }

    [Test]
    public void Configure_DefaultConfigurationSentryJavaNotPresent_LogsErrorAndReturns()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo)
        {
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug,
            DiagnosticLogger = _logger
        };
        _testSentryJava.SentryPresent = false;

        SentryNativeAndroid.Configure(options);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Android SDK is missing.")));

        Assert.Null(options.ScopeObserver);
    }

    [Test]
    public void Configure_NativeAlreadyInitialized_LogsAndConfigures()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo)
        {
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug,
            DiagnosticLogger = _logger
        };

        _testSentryJava.Enabled = true;

        SentryNativeAndroid.Configure(options);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Debug &&
            log.message.Contains("The Android SDK is already initialized")));

        Assert.NotNull(options.ScopeObserver);
    }

    [Test]
    public void Configure_NativeInitFails_LogsErrorAndReturns()
    {
        var options = new SentryUnityOptions(_sentryUnityInfo)
        {
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug,
            DiagnosticLogger = _logger
        };
        _testSentryJava.Enabled = false;
        _testSentryJava.InitSuccessful = false;

        SentryNativeAndroid.Configure(options);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Failed to initialize Android Native Support")));

        Assert.Null(options.ScopeObserver);
    }
}
