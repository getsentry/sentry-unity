using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata.Ecma335;
using System.Text.Json;
using Sentry.Extensibility;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sentry.Unity
{
    internal class ViewHierarchyAttachment : Attachment
    {
        public ViewHierarchyAttachment(IAttachmentContent  content) :
            base(AttachmentType.Default, content, "view-hierarchy.json", "application/json") { }
    }

    internal class ViewHierarchyAttachmentContent : IAttachmentContent
    {
        private readonly SentryMonoBehaviour _behaviour;
        private readonly SentryUnityOptions _options;

        public ViewHierarchyAttachmentContent(SentryUnityOptions options, SentryMonoBehaviour behaviour)
        {
            _behaviour = behaviour;
            _options = options;
        }

        public Stream GetStream()
        {
            // Note: we need to check explicitly that we're on the same thread. While Unity would throw otherwise
            // when capturing the screenshot, it would only do so on development builds. On release, it just crashes...
            if (!_behaviour.MainThreadData.IsMainThread())
            {
                _options.DiagnosticLogger?.LogDebug("Can't capture screenshots on other than main (UI) thread.");
                return Stream.Null;
            }

            return CaptureViewHierarchy(10, 10);
        }

        internal Stream CaptureViewHierarchy(int maxDepth, int maxChildCount)
        {
            var rootGameObjects = new List<GameObject>();
            SceneManager.GetActiveScene().GetRootGameObjects(rootGameObjects);

            var root = new ViewHierarchyNode();
            foreach (var gameObject in rootGameObjects)
            {
                CreateNode(maxDepth, maxChildCount, root, gameObject.transform);
            }

            var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);
            root.WriteTo(writer, _options.DiagnosticLogger);

            writer.Flush();
            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        internal void CreateNode(
            int depth,
            int maxChildCount,
            ViewHierarchyNode parentNode,
            Transform transform)
        {
            depth--;
            if (depth <= 0)
            {
                return;
            }

            var position = transform.position;
            var node = new ViewHierarchyNode
            {
                Identifier = transform.name,
                X = position.x,
                Y = position.y,
                Z = position.z,
                Tag = transform.tag,
                Visibility = transform.gameObject.activeSelf
            };

            if (parentNode.Children is null)
            {
                parentNode.Children = new List<ViewHierarchyNode> { node };
            }
            else
            {
                parentNode.Children.Add(node);
            }

            for (var i = 0; i < Math.Min(transform.childCount, maxChildCount); i++)
            {
                CreateNode(depth, maxChildCount, node, transform.GetChild(i));
            }
        }
    }
}
