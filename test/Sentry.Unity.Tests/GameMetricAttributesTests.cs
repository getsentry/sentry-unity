using System.Linq;
using NUnit.Framework;
using Sentry.Unity.Metrics;

namespace Sentry.Unity.Tests;

public class GameMetricAttributesTests
{
    private static SceneManagerIntegrationTests.FakeSceneManager CreateSceneManager(string activeScene) =>
        new() { ActiveSceneName = activeScene };

    [Test]
    public void Current_ContainsActiveSceneName()
    {
        var sceneManager = CreateSceneManager("MainMenu");

        var attributes = new GameMetricAttributes(sceneManager);

        var map = attributes.Current.First(kvp => kvp.Key == "map");
        Assert.AreEqual("MainMenu", map.Value);
    }

    [Test]
    public void ActiveSceneChanged_RefreshesMapAttribute()
    {
        var sceneManager = CreateSceneManager("MainMenu");
        var attributes = new GameMetricAttributes(sceneManager);
        var current = attributes.Current;

        sceneManager.OnActiveSceneChanged(new SceneAdapter("MainMenu"), new SceneAdapter("Level1"));

        var map = attributes.Current.First(kvp => kvp.Key == "map");
        Assert.AreEqual("Level1", map.Value);
        Assert.AreSame(current, attributes.Current);
    }

    [Test]
    public void Dispose_StopsRefreshingOnSceneChange()
    {
        var sceneManager = CreateSceneManager("MainMenu");
        var attributes = new GameMetricAttributes(sceneManager);

        attributes.Dispose();
        sceneManager.OnActiveSceneChanged(new SceneAdapter("MainMenu"), new SceneAdapter("Level1"));

        var map = attributes.Current.First(kvp => kvp.Key == "map");
        Assert.AreEqual("MainMenu", map.Value);
    }
}
