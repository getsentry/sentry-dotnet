using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Sentry View Hierarchy.
    /// </summary>
    public sealed class ViewHierarchy : ISentryJsonSerializable
    {
        /// <summary>
        /// The rendering system this view hierarchy is capturing.
        /// </summary>
        public string RenderingSystem { get; set; }

        /// <summary>
        /// The elements or windows within the view hierarchy.
        /// </summary>
        public List<ViewHierarchyNode> Windows { get; } = new();

        /// <summary>
        /// Initializes an instance of <see cref="ViewHierarchy"/>
        /// </summary>
        /// <param name="renderingSystem">The rendering system</param>
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
}
