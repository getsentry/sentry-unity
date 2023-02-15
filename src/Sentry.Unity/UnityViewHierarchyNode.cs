using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using UnityEngine;

namespace Sentry.Unity
{
    internal class UnityViewHierarchyNode : IViewHierarchyNode
    {
        public string Type { get; set; }
        public List<IViewHierarchyNode>? Children { get; set; }

        public string? Tag { get; set; }
        public string? Position { get; set; }
        public string? Rotation { get; set; }
        public string? Scale { get; set; }
        public bool? Active { get; set; }

        public List<string>? Extras { get; set; }

        public UnityViewHierarchyNode(string name)
        {
            Type = name;
        }

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (!string.IsNullOrWhiteSpace(Tag))
            {
                writer.WriteString("tag", Tag);
            }

            if (!string.IsNullOrWhiteSpace(Position))
            {
                writer.WriteString("position", Position);
            }
            if (!string.IsNullOrWhiteSpace(Rotation))
            {
                writer.WriteString("rotation", Rotation);
            }
            if (!string.IsNullOrWhiteSpace(Scale))
            {
                writer.WriteString("scale", Scale);
            }

            if (Active is {} active)
            {
                writer.WriteString("active", active.ToString());
            }

            if (Children is { } children)
            {
                writer.WriteStartArray("children");
                foreach (var child in children)
                {
                    child.WriteTo(writer, logger);
                }
                writer.WriteEndArray();
            }

            if (Extras is { } extras)
            {
                writer.WriteStartArray("extras");
                foreach (var extra in extras)
                {
                    writer.WriteStringValue(extra);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
