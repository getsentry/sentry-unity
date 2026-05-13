using System;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

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
    public void Configure_AndroidNativeAnrEnabled_RemovesAnrIntegration()
    {
        var options = new SentryUnityOptions { AndroidNativeAnrEnabled = true };
        Assert.IsTrue(options.HasIntegration<AnrIntegration>()); // sanity

        SentryNativeAndroid.Configure(options);

        Assert.IsFalse(options.HasIntegration<AnrIntegration>());
    }

    [Test]
    public void Configure_AndroidNativeAnrDisabled_KeepsAnrIntegration()
    {
        var options = new SentryUnityOptions { AndroidNativeAnrEnabled = false };

        SentryNativeAndroid.Configure(options);

        Assert.IsTrue(options.HasIntegration<AnrIntegration>());
    }

    [Test]
    public void Configure_AndroidNativeAnrEnabled_StartsHeartbeat()
    {
        var options = new SentryUnityOptions { AndroidNativeAnrEnabled = true };
        AnrHeartbeat? built = null;
        var go = new GameObject(nameof(Configure_AndroidNativeAnrEnabled_StartsHeartbeat));
        try
        {
            var monoBehaviour = go.AddComponent<Sentry.Unity.Tests.Stubs.TestSentryMonoBehaviour>();
            SentryNativeAndroid.HeartbeatFactory = (java, opts) =>
            {
                built = new AnrHeartbeat(monoBehaviour, java, opts.AnrTimeout);
                return built;
            };

            SentryNativeAndroid.Configure(options);

            Assert.NotNull(built);
            Assert.IsTrue(monoBehaviour.StartCoroutineCalled);
        }
        finally
        {
            SentryNativeAndroid.HeartbeatFactory = null;
            SentryNativeAndroid.Heartbeat = null;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }

    [Test]
    public void Configure_AndroidNativeAnrDisabled_DoesNotStartHeartbeat()
    {
        var options = new SentryUnityOptions { AndroidNativeAnrEnabled = false };
        var built = false;
        SentryNativeAndroid.HeartbeatFactory = (_, _) =>
        {
            built = true;
            return null!;
        };
        try
        {
            SentryNativeAndroid.Configure(options);
            Assert.IsFalse(built);
        }
        finally
        {
            SentryNativeAndroid.HeartbeatFactory = null;
            SentryNativeAndroid.Heartbeat = null;
        }
    }

    [Test]
    public void Close_StopsHeartbeat()
    {
        var options = new SentryUnityOptions { AndroidNativeAnrEnabled = true };
        var go = new GameObject(nameof(Close_StopsHeartbeat));
        try
        {
            var monoBehaviour = go.AddComponent<Sentry.Unity.Tests.Stubs.TestSentryMonoBehaviour>();
            SentryNativeAndroid.HeartbeatFactory = (java, opts) =>
                new AnrHeartbeat(monoBehaviour, java, opts.AnrTimeout);

            SentryNativeAndroid.Configure(options);
            Assert.IsNotNull(SentryNativeAndroid.Heartbeat); // sanity
            var stopCountBefore = monoBehaviour.StopCoroutineCallCount;

            SentryNativeAndroid.Close(options);

            Assert.IsNull(SentryNativeAndroid.Heartbeat);
            Assert.AreEqual(stopCountBefore + 1, monoBehaviour.StopCoroutineCallCount);
        }
        finally
        {
            SentryNativeAndroid.HeartbeatFactory = null;
            SentryNativeAndroid.Heartbeat = null;
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
