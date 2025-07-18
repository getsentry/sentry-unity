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
        public SentryOptions SentryOptions { get; set; }

        public Fixture() => SentryOptions = new SentryOptions { DiagnosticLogger = Logger };

        public TraceGenerationIntegration GetSut() => new(SentryMonoBehaviour, SceneManager);
    }

    private readonly Fixture _fixture = new();

    [SetUp]
    public void SetUp()
    {
        _fixture.TestHub = new TestHub();
        Sentry.SentrySdk.UseHub(_fixture.TestHub);
    }

    [Test]
    public void TraceGeneration_OnRegister_GeneratesInitialTrace()
    {
        // Arrange
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
    public void TraceGeneration_OnApplicationResume_GeneratesNewTrace()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.Register(_fixture.TestHub, _fixture.SentryOptions);
        var initialCallsCount = _fixture.TestHub.ConfigureScopeCalls.Count;

        // Act
        _fixture.SentryMonoBehaviour.ResumeApplication();

        // Assert
        // Calling 'Register' already generated a trace, so we expect 1+1 calls to ConfigureScope
        Assert.AreEqual(initialCallsCount + 1, _fixture.TestHub.ConfigureScopeCalls.Count);
        var configureScope = _fixture.TestHub.ConfigureScopeCalls.Last();
        var scope = new Scope(_fixture.SentryOptions);
        var initialPropagationContext = scope.PropagationContext;
        configureScope(scope);

        Assert.AreNotEqual(initialPropagationContext, scope.PropagationContext);
    }

    [Test]
    public void TraceGeneration_OnActiveSceneChange_GeneratesNewTrace()
    {
        // Arrange
        var sut = _fixture.GetSut();
        sut.Register(_fixture.TestHub, _fixture.SentryOptions);
        var initialCallsCount = _fixture.TestHub.ConfigureScopeCalls.Count;

        // Act
        _fixture.SceneManager.OnActiveSceneChanged(new SceneAdapter("from scene name"), new SceneAdapter("to scene name"));

        // Assert
        // Calling 'Register' already generated a trace, so we expect 1+1 calls to ConfigureScope
        Assert.AreEqual(initialCallsCount + 1, _fixture.TestHub.ConfigureScopeCalls.Count);
        var configureScope = _fixture.TestHub.ConfigureScopeCalls.Last();
        var scope = new Scope(_fixture.SentryOptions);
        var initialPropagationContext = scope.PropagationContext;
        configureScope(scope);

        Assert.AreNotEqual(initialPropagationContext, scope.PropagationContext);
    }

    internal class TestSentryMonoBehaviour : ISentryMonoBehaviour
    {
        public event Action? ApplicationResuming;

        public void ResumeApplication() => ApplicationResuming?.Invoke();
    }
}
