using System;
using UnityEngine.SceneManagement;

namespace Sentry.Unity;

// Accessors if UnityEngine.Scene do P/Invoke so we should map what we need only
internal readonly struct SceneAdapter
{
    public string Name { get; }
    public SceneAdapter(string name) => Name = name;
}

internal interface ISceneManager
{
    public event Action<SceneAdapter, LoadSceneMode> SceneLoaded;
    public event Action<SceneAdapter> SceneUnloaded;
    public event Action<SceneAdapter, SceneAdapter> ActiveSceneChanged;
}

internal sealed class SceneManagerAdapter : ISceneManager
{
    public event Action<SceneAdapter, LoadSceneMode>? SceneLoaded;
    public event Action<SceneAdapter>? SceneUnloaded;
    public event Action<SceneAdapter, SceneAdapter>? ActiveSceneChanged;

    public static readonly SceneManagerAdapter Instance = new();

    private SceneManagerAdapter()
    {
        SceneManager.sceneLoaded += (scene, mode)
            => SceneLoaded?.Invoke(new SceneAdapter(scene.name), mode);

        SceneManager.sceneUnloaded += scene
            => SceneUnloaded?.Invoke(new SceneAdapter(scene.name));

        SceneManager.activeSceneChanged += (sceneFrom, sceneTo) =>
        {
            ActiveSceneChanged?.Invoke(
                new SceneAdapter(sceneFrom.name),
                new SceneAdapter(sceneTo.name));
        };
    }
}
