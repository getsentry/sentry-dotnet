using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Sentry View Hierarchy.
    /// </summary>
    public sealed class ViewHierarchy : IJsonSerializable
    {
        /// <summary>
        /// The rendering system this view hierarchy is capturing.
        /// </summary>
        public string RenderingSystem { get; set; }

        /// <summary>
        /// The elements or windows within the view hierarchy.
        /// </summary>
        public List<ViewHierarchyNode> Windows { get; } = new();

        public ViewHierarchy(string renderingSystem)
        {
            RenderingSystem = renderingSystem;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteStringIfNotWhiteSpace("rendering_system", RenderingSystem);

            writer.WriteStartArray("windows");
            foreach (var window in Windows)
            {
                window.WriteTo(writer, logger);
            }
            writer.WriteEndArray();

            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Sentry View Hierarchy Node Interface
    /// </summary>
    public abstract class ViewHierarchyNode : IJsonSerializable
    {
        private List<ViewHierarchyNode>? _children;

        /// <summary>
        /// The type of the element represented by this node.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The child nodes
        /// </summary>
        public List<ViewHierarchyNode> Children
        {
            get => _children ??= new List<ViewHierarchyNode>();
            set => _children = value;
        }

        protected ViewHierarchyNode(string type)
        {
            Type = type;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            WriteAdditionalProperties(writer, logger);

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

        protected abstract void WriteAdditionalProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger);
    }
}
