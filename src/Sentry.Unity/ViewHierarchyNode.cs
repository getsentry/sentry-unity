using System.Collections.Generic;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry.Unity
{
    public class ViewHierarchyNode : IJsonSerializable
    {
        public string? Type { get; set; }
        public string? Identifier { get; set; }
        public string? Tag { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Z { get; set; }
        public bool? Visibility { get; set; }
        public List<ViewHierarchyNode>? Children { get; set; }
        // public Dictionary<string, object>? Unknown { get; set; }

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            if (Identifier is { } identifier)
            {
                writer.WriteString("identifier", identifier);
            }

            if (Tag is { } tag)
            {
                writer.WriteString("tag", tag);
            }

            if (X is { } x)
            {
                writer.WriteNumber("x", x);
            }

            if (Y is { } y)
            {
                writer.WriteNumber("y", y);
            }

            if (Z is { } z)
            {
                writer.WriteNumber("z", z);
            }

            if (Visibility is { } visibility)
            {
                writer.WriteBoolean("visibility", visibility);
            }

            if (Children is {} children)
            {
                writer.WriteStartArray("children");
                foreach (var child in children)
                {
                    child.WriteTo(writer, logger);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }
}
