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
        var options = new SentryUnityOptions();

        SentryNativeAndroid.Configure(options);

        Assert.IsAssignableFrom<AndroidJavaScopeObserver>(options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_SetsCrashedLastRun()
    {
        var options = new SentryUnityOptions();

        SentryNativeAndroid.Configure(options);

        Assert.IsNotNull(options.CrashedLastRun);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_ObserverIsNull()
    {
        var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };

        SentryNativeAndroid.Configure(options);

        Assert.Null(options.ScopeObserver);
    }

    [Test]
    public void Configure_DefaultConfiguration_EnablesScopeSync()
    {
        var options = new SentryUnityOptions();

        SentryNativeAndroid.Configure(options);

        Assert.True(options.EnableScopeSync);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DisabledScopeSync()
    {
        var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };

        SentryNativeAndroid.Configure(options);

        Assert.False(options.EnableScopeSync);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void Configure_IL2CPP_ReInitializesNativeBackend(bool il2cpp, bool expectedReinstall)
    {
        var options = new SentryUnityOptions(unityInfo: new TestUnityInfo { IL2CPP = il2cpp });

        Assert.False(_reinstallCalled); // Sanity check

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual(expectedReinstall, _reinstallCalled);
    }

    [Test]
    public void Configure_NativeAndroidSupportDisabled_DoesNotReInitializeNativeBackend()
    {
        var options = new SentryUnityOptions { AndroidNativeSupportEnabled = false };

        SentryNativeAndroid.Configure(options);

        Assert.False(_reinstallCalled);
    }

    [Test]
    public void AppHangHeartbeat_InvokesNativeHeartbeat()
    {
        var heartbeatCalled = false;
        var originalStrategy = Interlocked.Exchange(ref SentryNative.AppHangHeartbeatStrategy,
            () => heartbeatCalled = true);

        try
        {
            SentryNative.AppHangHeartbeat();

            Assert.True(heartbeatCalled);
        }
        finally
        {
            Interlocked.Exchange(ref SentryNative.AppHangHeartbeatStrategy, originalStrategy);
        }
    }

    [Test]
    public void AppHangPause_InvokesNativePause()
    {
        var pauseCalled = false;
        var originalStrategy = Interlocked.Exchange(ref SentryNative.AppHangPauseStrategy,
            () => pauseCalled = true);

        try
        {
            SentryNative.AppHangPause();

            Assert.True(pauseCalled);
        }
        finally
        {
            Interlocked.Exchange(ref SentryNative.AppHangPauseStrategy, originalStrategy);
        }
    }

    [Test]
    public void Configure_InstallationIdReturned_SetsDefaultUserId()
    {
        var options = new SentryUnityOptions();
        _testSentryJava.InstallationId = "test-installation-id";

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual("test-installation-id", options.DefaultUserId);
    }

    [Test]
    public void Configure_NoInstallationIdReturned_DoesNotSetDefaultUserId()
    {
        var options = new SentryUnityOptions();
        _testSentryJava.InstallationId = string.Empty;

        SentryNativeAndroid.Configure(options);

        Assert.IsNull(options.DefaultUserId);
    }

    [Test]
    public void Configure_DefaultConfigurationSentryJavaNotPresent_LogsErrorAndReturns()
    {
        var options = new SentryUnityOptions
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
        var options = new SentryUnityOptions
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
        var options = new SentryUnityOptions
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
    [Test]
    public void Configure_ExternalAppInitialized_AttachesWithoutOwningIdentity()
    {
        var options = new SentryUnityOptions { AndroidNativeInitializationType = NativeInitializationType.External };
        _testSentryJava.Enabled = true;

        SentryNativeAndroid.Configure(options);

        Assert.False(_testSentryJava.InitCalled);
        Assert.IsInstanceOf<ExternalAndroidJavaScopeObserver>(options.ScopeObserver);
        Assert.IsNull(options.NativeSupportCloseCallback);
        Assert.False(options.AutoSessionTracking);
    }

    [Test]
    public void Configure_ExternalNotInitializedInMainProcess_SkipsNativeSupport()
    {
        var options = new SentryUnityOptions { AndroidNativeInitializationType = NativeInitializationType.External };
        _testSentryJava.Enabled = false;
        _testSentryJava.MainProcess = true;

        SentryNativeAndroid.Configure(options);

        Assert.False(_testSentryJava.InitCalled);
        Assert.Null(options.ScopeObserver);
    }

    [Test]
    public void Configure_ExternalNotInitializedInGameProcess_InitializesAndOwnsSdk()
    {
        var options = new SentryUnityOptions { AndroidNativeInitializationType = NativeInitializationType.External };
        _testSentryJava.Enabled = false;
        _testSentryJava.MainProcess = false;

        SentryNativeAndroid.Configure(options);

        Assert.True(_testSentryJava.InitCalled);
        Assert.IsInstanceOf<ExternalAndroidJavaScopeObserver>(options.ScopeObserver);
        Assert.NotNull(options.NativeSupportCloseCallback);
        Assert.False(options.AutoSessionTracking);
    }

    [Test]
    public void Configure_ExternalNotInitializedInGameProcess_ContinuesMainProcessTrace()
    {
        var options = new SentryUnityOptions { AndroidNativeInitializationType = NativeInitializationType.External };
        _testSentryJava.Enabled = false;
        _testSentryJava.MainProcess = false;
        _testSentryJava.MainProcessTraceHeader = "75302ac48a024bde9a3b3734a82e36c8-1000000000000000";

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual("75302ac48a024bde9a3b3734a82e36c8", _testSentryJava.Trace?.TraceId.ToString());
        Assert.AreEqual("1000000000000000", _testSentryJava.Trace?.SpanId.ToString());
    }

    [Test]
    public void Configure_External_UsesAppReleaseAndEnvironment()
    {
        var options = new SentryUnityOptions { AndroidNativeInitializationType = NativeInitializationType.External };
        _testSentryJava.Options["getRelease"] = "io.sentry.host@1.0+1";
        _testSentryJava.Options["getEnvironment"] = "staging";

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual("io.sentry.host@1.0+1", options.Release);
        Assert.AreEqual("staging", options.Environment);
    }

    [Test]
    public void Configure_ExternalReleaseAndEnvironmentSet_KeepsThem()
    {
        var options = new SentryUnityOptions
        {
            AndroidNativeInitializationType = NativeInitializationType.External,
            Release = "game@2.0",
            Environment = "game-env"
        };
        _testSentryJava.Options["getRelease"] = "io.sentry.host@1.0+1";
        _testSentryJava.Options["getEnvironment"] = "staging";

        SentryNativeAndroid.Configure(options);

        Assert.AreEqual("game@2.0", options.Release);
        Assert.AreEqual("game-env", options.Environment);
    }

    [Test]
    public void Configure_RuntimeAlreadyInitialized_KeepsOwnership()
    {
        var options = new SentryUnityOptions();
        _testSentryJava.Enabled = true;

        SentryNativeAndroid.Configure(options);

        Assert.IsNotInstanceOf<ExternalAndroidJavaScopeObserver>(options.ScopeObserver);
        Assert.NotNull(options.NativeSupportCloseCallback);
    }
}
