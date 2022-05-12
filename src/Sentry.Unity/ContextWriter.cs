using System;
using System.IO;
using System.Text.Json;
using Sentry.Extensibility;

namespace Sentry.Unity
{
    /// <summary>
    /// Allows synchronizing Context from .NET to native layers. It does so,
    /// by first passing converting the context object, e.g. Device, to JSON,
    /// and then parsing it with Utf8JsonReader, while invoking the abstract
    /// methods. The native layer should forward these to native calls.
    /// </summary>
    /// <remarks>
    /// The objects given to Write() are expected to produce a single-level
    /// map of key-value pairs. The "type" key must indicate the object name.
    /// </remarks>
    internal abstract class ContextWriter
    {
        public void Write(IJsonSerializable contextObject, IDiagnosticLogger? logger)
        {
            using var stream = new MemoryStream();
            using var writer = new Utf8JsonWriter(stream);

            contextObject.WriteTo(writer, logger);
            writer.Flush();

            var options = new JsonReaderOptions
            {
                MaxDepth = 1, // We expect a flat object
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            };

            var json = stream.GetBuffer();
            var reader = new Utf8JsonReader(json, options);

            string? type = null;
            string? property = null;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                        StartObject();
                        break;
                    case JsonTokenType.EndObject:
                        if (type is null)
                        {
                            logger?.LogWarning("ContextWriter: can't write the context object - 'type' not set.");
                        }
                        else
                        {
                            EndObject(type!);
                        }
                        // No more data expected after an object end.
                        // We must `return` here or an exception would be thrown:
                        // "JsonReaderException: '0x00' is invalid after a single JSON value. Expected end of data."
                        return;
                    case JsonTokenType.PropertyName:
                        property = reader.GetString()!;
                        break;
                    case JsonTokenType.String:
                        AddProperty(property!, reader.GetString()!);
                        break;
                    case JsonTokenType.Number:
                        AddProperty(property!, reader.GetInt64());
                        break;
                    case JsonTokenType.True:
                    case JsonTokenType.False:
                        AddProperty(property!, reader.GetBoolean());
                        break;
                    case JsonTokenType.Null:
                    case JsonTokenType.Comment:
                        // skip silently
                        break;
                    case JsonTokenType.StartArray:
                    case JsonTokenType.EndArray:
                    case JsonTokenType.None:
                    default:
                        logger?.LogWarning("ContextWriter({0}): encountered an unsupported JSON token {1}", type, reader.TokenType);
                        break;
                }
            }
        }

        protected abstract void StartObject();
        protected abstract void AddProperty(string name, string value);
        protected abstract void AddProperty(string name, long value);
        protected abstract void AddProperty(string name, bool value);
        protected abstract void EndObject(string name);
    }
}
