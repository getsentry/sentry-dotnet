using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    public class ViewHierarchy : IJsonSerializable
    {
        public string RenderingSystem { get; set; } = string.Empty;
        public List<IJsonSerializable>? Children { get; set; }

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
        public string Type { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public List<IJsonSerializable>? Children { get; set; }

        public virtual void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteStringIfNotWhiteSpace("type", Type);
            writer.WriteStringIfNotWhiteSpace("identifier", Identifier);

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
