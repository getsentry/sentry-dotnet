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
        public string RenderingSystem { get; set; } = string.Empty;

        /// <summary>
        /// The elements or windows within the view hierarchy.
        /// </summary>
        public List<IViewHierarchyNode> Windows { get; set; } = new();

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
    public interface IViewHierarchyNode : IJsonSerializable
    {
        /// <summary>
        /// The type of the element represented by this node.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// The child nodes
        /// </summary>
        public List<IViewHierarchyNode>? Children { get; set; }
    }
}
