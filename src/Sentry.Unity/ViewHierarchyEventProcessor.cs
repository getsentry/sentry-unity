using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity;

public class ViewHierarchyEventProcessor : ISentryEventProcessorWithHint
{
    private readonly SentryUnityOptions _options;

    public ViewHierarchyEventProcessor(SentryUnityOptions sentryOptions)
    {
        _options = sentryOptions;
    }

    public SentryEvent? Process(SentryEvent @event)
    {
        return @event;
    }

    public SentryEvent? Process(SentryEvent @event, SentryHint hint)
    {
        if (!MainThreadData.IsMainThread())
        {
            _options.LogDebug("Hierarchy capture skipped. Can't capture hierarchy on other than the main thread.");
            return @event;
        }

        if (_options.BeforeCaptureViewHierarchyInternal?.Invoke(@event) is false)
        {
            _options.LogInfo("Hierarchy capture skipped by BeforeCaptureViewHierarchy callback.");
            return @event;
        }

        var viewHierarchy = CreateViewHierarchy(
            _options.MaxViewHierarchyRootObjects,
            _options.MaxViewHierarchyObjectChildCount,
            _options.MaxViewHierarchyDepth);

        if (_options.BeforeSendViewHierarchyInternal != null)
        {
            viewHierarchy = _options.BeforeSendViewHierarchyInternal(viewHierarchy, @event);

            if (viewHierarchy == null)
            {
                _options.LogInfo("View hierarchy discarded by BeforeSendViewHierarchy callback.");
                return @event;
            }
        }

        var bytes = SerializeViewHierarchy(viewHierarchy);
        hint.AddAttachment(bytes, "view-hierarchy.json", AttachmentType.ViewHierarchy, "application/json");

        return @event;
    }

    internal byte[] SerializeViewHierarchy(ViewHierarchy viewHierarchy)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        viewHierarchy.WriteTo(writer, _options.DiagnosticLogger);

        writer.Flush();
        return stream.ToArray();
    }

    internal ViewHierarchy CreateViewHierarchy(int maxRootGameObjectCount, int maxChildCount, int maxDepth)
    {
        var rootGameObjects = new List<GameObject>();
        var scene = SceneManager.GetActiveScene();
        scene.GetRootGameObjects(rootGameObjects);

        // Consider the root a 'scene'.
        var root = new UnityViewHierarchyNode(scene.name);
        var viewHierarchy = new ViewHierarchy("Unity");
        viewHierarchy.Windows.Add(root);

        var rootElementCount = Math.Min(rootGameObjects.Count, maxRootGameObjectCount);
        for (var i = 0; i < rootElementCount; i++)
        {
            CreateNode(maxDepth, maxChildCount, root, rootGameObjects[i].transform);
        }

        return viewHierarchy;
    }

    internal void CreateNode(int remainingDepth, int maxChildCount, ViewHierarchyNode parentNode, Transform transform)
    {
        var components = new List<Component>();
        transform.GetComponents(components);
        var extras = new List<string>(components.Count);
        foreach (var component in components)
        {
            if (component?.GetType().Name is { } componentName)
            {
                extras.Add(componentName);
            }
        }
        var node = new UnityViewHierarchyNode(transform.name)
        {
            Tag = transform.tag,
            Position = transform.position.ToString(),
            Rotation = transform.rotation.eulerAngles.ToString(),
            Scale = transform.localScale.ToString(),
            Active = transform.gameObject.activeSelf,
            Extras = extras,
        };

        parentNode.Children.Add(node);

        remainingDepth--;
        if (remainingDepth <= 0)
        {
            return;
        }

        var childCount = Math.Min(transform.childCount, maxChildCount);
        for (var i = 0; i < childCount; i++)
        {
            CreateNode(remainingDepth, maxChildCount, node, transform.GetChild(i));
        }
    }
}
