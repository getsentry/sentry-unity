using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;

namespace Sentry.Unity.Android.Tests;

public class SentryNativeAndroidTests
{
    private bool _reinstallCalled;
    private Action? _originalReinstallSentryNativeBackendStrategy;
    private Action _fakeReinstallSentryNativeBackendStrategy;
    private TestUnityInfo _sentryUnityInfo = null!;
    private TestSentryJava _testSentryJava = null!;
    private TestLogger _logger = new();
    private SentryUnityOptions _options = null!;

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

        SentryNativeAndroid.JniExecutor ??= new JniExecutor(_logger);
        _testSentryJava = new TestSentryJava();
        SentryNativeAndroid.SentryJava = _testSentryJava;

        _options = new SentryUnityOptions
        {
            Debug = true,
            DiagnosticLevel = SentryLevel.Debug,
            DiagnosticLogger = _logger
        };
    }

    [TearDown]
    public void TearDown()
    {
        _fakeReinstallSentryNativeBackendStrategy =
            Interlocked.Exchange(ref SentryNative.ReinstallSentryNativeBackendStrategy!,
                _originalReinstallSentryNativeBackendStrategy)!;
    }

    [Test]
    public void Configure_DefaultConfiguration_SetsScopeObserver()
    {
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.IsAssignableFrom<AndroidJavaScopeObserver>(_options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_SetsCrashedLastRun()
    {
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.IsNotNull(_options.CrashedLastRun);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
    {
        _options.AndroidNativeSupportEnabled = false;
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.Null(_options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_EnablesScopeSync()
    {
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.True(_options.EnableScopeSync);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
    {
        _options.AndroidNativeSupportEnabled = false;
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.False(_options.EnableScopeSync);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void Configure_IL2CPP_ReInitializesNativeBackend(bool il2cpp, bool expectedReinstall)
    {
        _sentryUnityInfo.IL2CPP = il2cpp;
        Assert.False(_reinstallCalled); // Sanity check

        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);

        Assert.AreEqual(expectedReinstall, _reinstallCalled);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
    {
        _options.AndroidNativeSupportEnabled = false;
        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.False(_reinstallCalled);
    }

    [Test]
    public void Configure_NoInstallationIdReturned_SetsNewDefaultUserId()
    {
        _testSentryJava.InstallationId = string.Empty;

        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);
        Assert.False(string.IsNullOrEmpty(_options.DefaultUserId));
    }

    [Test]
    public void Configure_DefaultConfigurationSentryJavaNotPresent_LogsErrorAndReturns()
    {
        _testSentryJava.SentryPresent = false;

        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Sentry Java SDK is missing.")));

        Assert.Null(_options.ScopeObserver);
    }

    [Test]
    public void Configure_NativeAlreadyInitialized_LogsAndConfigures()
    {
        _testSentryJava.Enabled = true;

        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Debug &&
            log.message.Contains("The Android SDK is already initialized")));

        Assert.NotNull(_options.ScopeObserver);
    }

    [Test]
    public void Configure_NativeInitFails_LogsErrorAndReturns()
    {
        _testSentryJava.Enabled = false;
        _testSentryJava.InitSuccessful = false;

        SentryNativeAndroid.Configure(_options, _sentryUnityInfo);

        Assert.IsTrue(_logger.Logs.Any(log =>
            log.logLevel == SentryLevel.Error &&
            log.message.Contains("Failed to initialize Android Native Support")));

        Assert.Null(_options.ScopeObserver);
    }
}
