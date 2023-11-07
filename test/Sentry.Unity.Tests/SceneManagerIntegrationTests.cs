using System;
using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Integrations;
using Sentry.Unity.Tests.Stubs;
using UnityEngine.SceneManagement;

namespace Sentry.Unity.Tests
{
    public class SceneManagerIntegrationTests
    {
        [Test]
        public void SceneLoaded_DisabledHub_NoCrumbAdded()
        {
            _fixture.TestHub = new TestHub(false);
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            _fixture.SceneManager.OnSceneLoaded(default, default);

            Assert.Zero(_fixture.TestHub.ConfigureScopeCalls.Count);
        }

        [Test]
        public void SceneLoaded_EnabledHub_CrumbAdded()
        {
            _fixture.TestHub = new TestHub();
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            const string? sceneName = "scene name";
            var expectedScene = new SceneAdapter(sceneName);
            _fixture.SceneManager.OnSceneLoaded(expectedScene,
                LoadSceneMode.Additive);

            var configureScope = _fixture.TestHub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var actualCrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual($"Scene '{sceneName}' was loaded", actualCrumb.Message);
            Assert.AreEqual("scene.loaded", actualCrumb.Category);
        }

        [Test]
        public void SceneUnloaded_DisabledHub_NoCrumbAdded()
        {
            _fixture.TestHub = new TestHub(false);
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            _fixture.SceneManager.OnSceneUnloaded(default);

            Assert.Zero(_fixture.TestHub.ConfigureScopeCalls.Count);
        }

        [Test]
        public void SceneUnloaded_EnabledHub_CrumbAdded()
        {
            _fixture.TestHub = new TestHub();
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            const string? sceneName = "scene name";
            var expectedScene = new SceneAdapter(sceneName);
            _fixture.SceneManager.OnSceneUnloaded(expectedScene);

            var configureScope = _fixture.TestHub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var actualCrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual($"Scene '{sceneName}' was unloaded", actualCrumb.Message);
            Assert.AreEqual("scene.unloaded", actualCrumb.Category);
        }

        [Test]
        public void ActiveSceneChanged_DisabledHub_NoCrumbAdded()
        {
            _fixture.TestHub = new TestHub(false);
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            _fixture.SceneManager.OnActiveSceneChanged(default, default);

            Assert.Zero(_fixture.TestHub.ConfigureScopeCalls.Count);
        }

        [Test]
        public void ActiveSceneChanged_EnabledHub_CrumbAdded()
        {
            _fixture.TestHub = new TestHub();
            var sut = _fixture.GetSut();

            sut.Register(_fixture.TestHub, _fixture.SentryOptions);
            const string? fromSceneName = "from scene name";
            const string? toSceneName = "to scene name";
            var expectedFromScene = new SceneAdapter(fromSceneName);
            var expectedToScene = new SceneAdapter(toSceneName);
            _fixture.SceneManager.OnActiveSceneChanged(expectedFromScene, expectedToScene);

            var configureScope = _fixture.TestHub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var actualCrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual($"Changed active scene '{expectedFromScene.Name}' to '{expectedToScene.Name}'", actualCrumb.Message);
            Assert.AreEqual("scene.changed", actualCrumb.Category);
            Assert.Null(actualCrumb.Data);
        }

        private class Fixture
        {
            public FakeSceneManager SceneManager { get; set; } = new();
            public TestHub TestHub { get; set; } = new();
            public SentryOptions SentryOptions { get; set; } = new();
            public SceneManagerIntegration GetSut() => new(SceneManager);
        }

        private readonly Fixture _fixture = new();

        private class FakeSceneManager : ISceneManager
        {
            public event Action<SceneAdapter, LoadSceneMode>? SceneLoaded;
            public event Action<SceneAdapter>? SceneUnloaded;
            public event Action<SceneAdapter, SceneAdapter>? ActiveSceneChanged;
            public void OnSceneLoaded(SceneAdapter scene, LoadSceneMode mode) => SceneLoaded?.Invoke(scene, mode);
            public void OnSceneUnloaded(SceneAdapter scene) => SceneUnloaded?.Invoke(scene);
            public void OnActiveSceneChanged(SceneAdapter fromScene, SceneAdapter toScene) => ActiveSceneChanged?.Invoke(fromScene, toScene);
        }
    }
}
