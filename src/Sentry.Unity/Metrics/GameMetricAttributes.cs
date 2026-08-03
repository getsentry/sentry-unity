using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sentry.Unity.Metrics;

/// <summary>
/// Caches the set of attributes attached to every auto-collected game metric. The hardware
/// attributes are captured once; the active scene name is refreshed whenever the scene changes.
/// </summary>
internal class GameMetricAttributes
{
    private readonly ISceneManager _sceneManager;
    private readonly Action<SceneAdapter, SceneAdapter> _onActiveSceneChanged;

    private readonly List<KeyValuePair<string, object>> _attributes = new(7);

    public GameMetricAttributes(ISceneManager? sceneManager = null)
    {
        _sceneManager = sceneManager ?? SceneManagerAdapter.Instance;

        Add("gpu.name", MainThreadData.GraphicsDeviceName);
        Add("cpu.cores", MainThreadData.ProcessorCount);
        // Unity reports the system memory size in megabytes.
        Add("ram.gb", MainThreadData.SystemMemorySize.HasValue ? MainThreadData.SystemMemorySize.Value / 1024 : null);
        Add("res.x", Screen.width);
        Add("res.y", Screen.height);
        Add("platform", Application.platform.ToString());

        var mapAttributeIndex = _attributes.Count;
        _attributes.Add(new KeyValuePair<string, object>("map", _sceneManager.GetActiveScene().Name));

        _onActiveSceneChanged = (_, to) => _attributes[mapAttributeIndex] = new KeyValuePair<string, object>("map", to.Name);
        _sceneManager.ActiveSceneChanged += _onActiveSceneChanged;
    }

    /// <summary>
    /// The attributes to attach to metrics emitted for the current frame/scene.
    /// </summary>
    public IReadOnlyList<KeyValuePair<string, object>> Current => _attributes;

    private void Add(string key, object? value)
    {
        if (value is not null)
        {
            _attributes.Add(new KeyValuePair<string, object>(key, value));
        }
    }

    public void Dispose() => _sceneManager.ActiveSceneChanged -= _onActiveSceneChanged;
}
