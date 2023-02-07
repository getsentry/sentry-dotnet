using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    public class ViewHierarchy : IJsonSerializable
    {
        public string? RenderingSystem { get; set; }
        public List<ViewHierarchyNode>? Children { get; set; }

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteStringIfNotWhiteSpace("rendering_system", RenderingSystem);

            if (Children is {} children)
            {
                writer.WriteStartArray("windows");
                foreach (var child in children)
                {
                    child.WriteTo(writer, logger);
                }
                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }
    }

    public class ViewHierarchyNode : IJsonSerializable
    {
        public string? Type { get; set; }
        public string? Identifier { get; set; }
        public string? Tag { get; set; }
        public float? X { get; set; }
        public float? Y { get; set; }
        public float? Z { get; set; }
        public float? Width { get; set; }
        public float? Height { get; set; }
        public bool? Visible { get; set; }
        public List<ViewHierarchyNode>? Children { get; set; }
        // public Dictionary<string, object>? Unknown { get; set; }

        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteStringIfNotWhiteSpace("type", Type);
            writer.WriteStringIfNotWhiteSpace("identifier", Identifier);
            writer.WriteStringIfNotWhiteSpace("tag", Tag);
            writer.WriteNumberIfNotNull("x", X);
            writer.WriteNumberIfNotNull("y", Y);
            writer.WriteNumberIfNotNull("z", Z);
            writer.WriteNumberIfNotNull("width", Width);
            writer.WriteNumberIfNotNull("height", Height);
            writer.WriteBooleanIfNotNull("visible", Visible);

            if (Children is { } children)
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
