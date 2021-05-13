using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Tests.Stubs;
using UnityEngine.Events;
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
            var expectedScene = new Scene
            {
                name = sceneName,
            };
            _fixture.SceneManager.OnSceneLoaded(expectedScene,
                LoadSceneMode.Additive);

            var configureScope = _fixture.TestHub.ConfigureScopeCalls.Single();
            var scope = new Scope(_fixture.SentryOptions);
            configureScope(scope);
            var actualCrumb = scope.Breadcrumbs.Single();

            Assert.AreEqual($"Scene '{sceneName}' was loaded", actualCrumb.Message);
            Assert.AreEqual(sceneName, actualCrumb.Data!["name"]);
            // To assert the other fields we'd need to abstract the Scene (struct with extern) away
            // which would mean allocations on each call. For that reason doing only validation on the 'name'
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
            public event UnityAction<Scene, LoadSceneMode>? SceneLoaded;
            public event UnityAction<Scene>? SceneUnloaded;
            public event UnityAction<Scene, Scene>? ActiveSceneChanged;
            public void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SceneLoaded?.Invoke(scene, mode);
            public void OnSceneUnloaded(Scene scene) => SceneUnloaded?.Invoke(scene);
            public void OnActiveSceneChanged(Scene fromScene, Scene toScene) => ActiveSceneChanged?.Invoke(fromScene, toScene);
        }
    }
}
