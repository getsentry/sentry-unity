using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.SharedClasses;
using Sentry.Unity.Tests.Stubs;
using static Sentry.Unity.Tests.SceneManagerIntegrationTests;

namespace Sentry.Unity.Tests;

public class TraceGenerationIntegrationTests
{
    private class Fixture
    {
        public FakeSceneManager SceneManager { get; set; } = new();
        public TestSentryMonoBehaviour SentryMonoBehaviour { get; set; } = new();
        public TestHub TestHub { get; set; } = new();
        public TestLogger Logger { get; set; } = new();
        public SentryUnityOptions SentryOptions { get; set; }

        public Fixture() => SentryOptions = new SentryUnityOptions { DiagnosticLogger = Logger };

        public TraceGenerationIntegration GetSut() => new(SentryMonoBehaviour, SceneManager);
    }

    private readonly Fixture _fixture = new();

    [SetUp]
    public void SetUp()
    {
        _fixture.TestHub = new TestHub();
        Sentry.SentrySdk.UseHub(_fixture.TestHub);
    }

    [TestCase(0.0f, false)]
    [TestCase(0.0f, true)]
    [TestCase(1.0f, false)]
    public void Register_TracingDisabledOrAutoStartupTracesDisabled_GeneratesInitialTrace(float tracesSampleRate, bool autoStartupTraces)
    {
        // Arrange
        _fixture.SentryOptions.TracesSampleRate = tracesSampleRate;
        _fixture.SentryOptions.AutoStartupTraces = autoStartupTraces;
        var sut = _fixture.GetSut();

        // Act
        sut.Register(_fixture.TestHub, _fixture.SentryOptions);

        // Assert
        var configureScope = _fixture.TestHub.ConfigureScopeCalls.Single();
        var scope = new Scope(_fixture.SentryOptions);
        var initialPropagationContext = scope.PropagationContext;
        configureScope(scope);
        Assert.AreNotEqual(initialPropagationContext, scope.PropagationContext);
    }

    [Test]
    public void Register_TracingEnabledAndAutoStartupTracesEnabled_DoesNotGenerateInitialTrace()
    {
        // Arrange
        _fixture.SentryOptions.TracesSampleRate = 1.0f;
        _fixture.SentryOptions.AutoStartupTraces = true;
        var sut = _fixture.GetSut();

        // Act
        sut.Register(_fixture.TestHub, _fixture.SentryOptions);

        // Assert
        Assert.IsEmpty(_fixture.TestHub.ConfigureScopeCalls);
    }

    [Test]
    public void ActiveSceneChanged_DoesNotRegeneratePropagationContext()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.Register(_fixture.TestHub, _fixture.SentryOptions);

        // Capture the propagation context state after registration
        var scope = new Scope(_fixture.SentryOptions);
        foreach (var configureScope in _fixture.TestHub.ConfigureScopeCalls)
        {
            configureScope(scope);
        }
        var traceIdAfterRegistration = scope.PropagationContext.TraceId;
        var callCountAfterRegistration = _fixture.TestHub.ConfigureScopeCalls.Count;

        // Act
        _fixture.SceneManager.OnActiveSceneChanged(
            new SceneAdapter("from scene name"),
            new SceneAdapter("to scene name"));

        // Apply only the NEW calls that happened after scene change
        for (var i = callCountAfterRegistration; i < _fixture.TestHub.ConfigureScopeCalls.Count; i++)
        {
            _fixture.TestHub.ConfigureScopeCalls[i](scope);
        }

        // Assert
        Assert.AreEqual(traceIdAfterRegistration, scope.PropagationContext.TraceId,
            "TraceId should not change after scene change");
    }
}
