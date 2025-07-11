using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using UnityEngine;

namespace Sentry.Unity.Tests;

public class StartupTracingIntegrationTests
{
    private class Fixture
    {
        public TestHub Hub { get; set; } = null!;
        public SentryUnityOptions Options { get; set; } = null!;
        public TestApplication Application { get; set; } = null!;
        public TestLogger Logger { get; set; } = null!;

        public StartupTracingIntegration GetSut() => new();
    }

    private Fixture _fixture = null!;

    [SetUp]
    public void SetUp()
    {
        _fixture = new Fixture();
        _fixture.Hub = new TestHub();
        _fixture.Logger = new TestLogger();
        _fixture.Application = new TestApplication(isEditor: false, platform: RuntimePlatform.WindowsPlayer);
        _fixture.Options = new SentryUnityOptions
        {
            TracesSampleRate = 1.0f,
            AutoStartupTraces = true,
            DiagnosticLogger = _fixture.Logger
        };

        ResetStaticState();
    }

    [TearDown]
    public void TearDown()
    {
        ResetStaticState();
        if (SentrySdk.IsEnabled)
        {
            SentrySdk.Close();
        }
    }

    private void ResetStaticState()
    {
        StartupTracingIntegration.Application = null;
        StartupTracingIntegration.IsGameStartupFinished = false;
        StartupTracingIntegration.IsIntegrationRegistered = false;
    }

    [Test]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void IsStartupTracingAllowed_IsEditor_ReturnsExpected(bool isEditor, bool expected)
    {
        _fixture.Application.IsEditor = isEditor;
        _fixture.Application.Platform = isEditor ? RuntimePlatform.WindowsEditor : RuntimePlatform.WindowsPlayer;
        StartupTracingIntegration.Application = _fixture.Application;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        var result = StartupTracingIntegration.IsStartupTracingAllowed();

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(RuntimePlatform.WebGLPlayer, false)]
    [TestCase(RuntimePlatform.WindowsPlayer, true)]
    [TestCase(RuntimePlatform.Android, true)]
    [TestCase(RuntimePlatform.IPhonePlayer, true)]
    public void IsStartupTracingAllowed_Platform_ReturnsExpected(RuntimePlatform platform, bool expected)
    {
        _fixture.Application.IsEditor = false;
        _fixture.Application.Platform = platform;
        StartupTracingIntegration.Application = _fixture.Application;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        var result = StartupTracingIntegration.IsStartupTracingAllowed();

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, false)]
    public void IsStartupTracingAllowed_IntegrationRegistered_ReturnsExpected(bool isRegistered, bool expected)
    {
        _fixture.Application.IsEditor = false;
        _fixture.Application.Platform = RuntimePlatform.WindowsPlayer;
        StartupTracingIntegration.Application = _fixture.Application;

        if (isRegistered)
        {
            _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);
        }

        Assert.AreEqual(StartupTracingIntegration.IsIntegrationRegistered, isRegistered); // Sanity Check

        var result = StartupTracingIntegration.IsStartupTracingAllowed();

        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void IsStartupTracingAllowed_GameStartupFinished_ReturnsExpected(bool isFinished, bool expected)
    {
        _fixture.Application.IsEditor = false;
        _fixture.Application.Platform = RuntimePlatform.WindowsPlayer;
        StartupTracingIntegration.Application = _fixture.Application;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);
        StartupTracingIntegration.IsGameStartupFinished = isFinished;

        var result = StartupTracingIntegration.IsStartupTracingAllowed();

        Assert.AreEqual(expected, result);
    }

    [Test]
    public void Register_WithTracesSampleRateZero_DoesNotSetIntegrationRegistered()
    {
        _fixture.Options.TracesSampleRate = 0.0f;

        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        Assert.IsFalse(StartupTracingIntegration.IsIntegrationRegistered);
    }

    [Test]
    public void Register_WithAutoStartupTracesDisabled_DoesNotSetIntegrationRegistered()
    {
        _fixture.Options.AutoStartupTraces = false;

        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        Assert.IsFalse(StartupTracingIntegration.IsIntegrationRegistered);
    }

    [Test]
    public void Register_WithValidOptions_SetsIntegrationRegistered()
    {
        _fixture.Options.TracesSampleRate = 1.0f;
        _fixture.Options.AutoStartupTraces = true;

        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        Assert.IsTrue(StartupTracingIntegration.IsIntegrationRegistered);
    }

    [Test]
    public void StartTracing_WhenNotAllowed_DoesNotCreateTransaction()
    {
        _fixture.Application.Platform = RuntimePlatform.WebGLPlayer;
        StartupTracingIntegration.Application = _fixture.Application;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);
        StartupTracingIntegration.IsGameStartupFinished = true;

        StartupTracingIntegration.StartTracing();
        StartupTracingIntegration.AfterSceneLoad(); // Contains the finish

        Assert.IsEmpty(_fixture.Hub.CapturedTransactions);
    }

    [Test]
    public void StartupSequence_CallsInOrder_CreatesAndFinishesTransactionCorrectly()
    {
        SentrySdk.UseHub(_fixture.Hub);
        _fixture.Application.IsEditor = false;
        _fixture.Application.Platform = RuntimePlatform.WindowsPlayer;
        StartupTracingIntegration.Application = _fixture.Application;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        StartupTracingIntegration.StartTracing();
        StartupTracingIntegration.AfterAssembliesLoaded();
        StartupTracingIntegration.BeforeSplashScreen();
        StartupTracingIntegration.BeforeSceneLoad();
        StartupTracingIntegration.AfterSceneLoad();

        // Verify that ConfigureScope was called at least twice (start transaction, finish transaction)
        Assert.GreaterOrEqual(_fixture.Hub.ConfigureScopeCalls.Count, 2); // Sanity Check

        var mockScope = new Scope(_fixture.Options);

        // Apply the transaction start
        _fixture.Hub.ConfigureScopeCalls.First().Invoke(mockScope);

        Assert.IsNotNull(mockScope.Transaction);
        Assert.AreEqual("runtime.initialization", mockScope.Transaction!.Name);
        Assert.AreEqual("app.start", mockScope.Transaction.Operation);
        Assert.IsFalse(mockScope.Transaction.IsFinished);

        // Dragging it out here because it gets cleared from the scope
        var transaction = mockScope.Transaction;
        // Apply the transaction finish
        _fixture.Hub.ConfigureScopeCalls.Last().Invoke(mockScope);

        Assert.IsTrue(transaction.IsFinished);
    }

    [Test]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void AfterSceneLoad_SetsGameStartupFinished(bool isTracingAllowed, bool expectedIsGameStartupFinished)
    {
        _fixture.Application.IsEditor = !isTracingAllowed;
        _fixture.GetSut().Register(_fixture.Hub, _fixture.Options);

        StartupTracingIntegration.AfterSceneLoad();

        Assert.AreEqual(StartupTracingIntegration.IsGameStartupFinished, expectedIsGameStartupFinished);
    }
}
