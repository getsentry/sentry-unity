using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity;

internal class UnityViewHierarchyAttachmentContent : IAttachmentContent
{
    private readonly SentryUnityOptions _options;

    public UnityViewHierarchyAttachmentContent(SentryUnityOptions options)
    {
        _options = options;
    }

    public Stream GetStream()
    {
        // Note: we need to check explicitly that we're on the same thread. While Unity would throw otherwise
        // when capturing the screenshot, it would only do so on development builds. On release, it just crashes...
        if (!_options.IsMainThread())
        {
            _options.DiagnosticLogger?.LogDebug("Can't capture screenshots on other than main (UI) thread.");
            return Stream.Null;
        }

        return CaptureViewHierarchy();
    }

    internal Stream CaptureViewHierarchy()
    {
        var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);

        var viewHierarchy = CreateViewHierarchy(
            _options.MaxViewHierarchyRootObjects,
            _options.MaxViewHierarchyObjectChildCount,
            _options.MaxViewHierarchyDepth);
        viewHierarchy.WriteTo(writer, _options.DiagnosticLogger);

        writer.Flush();
        stream.Seek(0, SeekOrigin.Begin);

        return stream;
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
        var node = new UnityViewHierarchyNode(transform.name)
        {
            Tag = transform.tag,
            Position = transform.position.ToString(),
            Rotation = transform.rotation.eulerAngles.ToString(),
            Scale = transform.localScale.ToString(),
            Active = transform.gameObject.activeSelf,
            Extras = components.Select(e => e.GetType().ToString()).ToList()
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
