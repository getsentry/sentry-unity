using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry.Unity;

/// <summary>
/// Caches the set of attributes attached to every auto-collected game metric. The hardware
/// attributes are captured once; the active scene name is refreshed whenever the scene changes.
/// </summary>
internal class GameMetricAttributes
{
    private readonly ISceneManager _sceneManager;
    private readonly Action<SceneAdapter, SceneAdapter> _onActiveSceneChanged;

    // Built and read on the Unity main thread - both the per-frame sampling and the scene-changed
    // callback run there - so no synchronization is required.
    private KeyValuePair<string, object>[] _attributes;

    public GameMetricAttributes(ISceneManager? sceneManager = null)
    {
        _sceneManager = sceneManager ?? SceneManagerAdapter.Instance;
        _attributes = Build(_sceneManager.GetActiveScene().Name);

        _onActiveSceneChanged = (_, to) => _attributes = Build(to.Name);
        _sceneManager.ActiveSceneChanged += _onActiveSceneChanged;
    }

    /// <summary>
    /// The attributes to attach to metrics emitted for the current frame/scene.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object>> Current => _attributes;

    private static KeyValuePair<string, object>[] Build(string? mapName)
    {
        var attributes = new List<KeyValuePair<string, object>>(7);

        void Add(string key, object? value)
        {
            if (value is not null)
            {
                attributes.Add(new KeyValuePair<string, object>(key, value));
            }
        }

        Add("gpu.name", MainThreadData.GraphicsDeviceName);
        Add("cpu.cores", MainThreadData.ProcessorCount);
        // Unity reports the system memory size in megabytes.
        Add("ram.gb", MainThreadData.SystemMemorySize.HasValue ? MainThreadData.SystemMemorySize.Value / 1024 : null);
        Add("res.x", Screen.width);
        Add("res.y", Screen.height);
        Add("map", mapName);
        Add("platform", Application.platform.ToString());

        return attributes.ToArray();
    }

    public void Dispose() => _sceneManager.ActiveSceneChanged -= _onActiveSceneChanged;
}
