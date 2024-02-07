using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// Sentry View Hierarchy Node
/// </summary>
public abstract class ViewHierarchyNode : ISentryJsonSerializable
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

    /// <summary>
    /// Initializes an instance of <see cref="ViewHierarchyNode"/>
    /// </summary>
    /// <param name="type">The type of node</param>
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

    /// <summary>
    /// Gets automatically called and writes additional properties during <see cref="WriteTo"/>
    /// </summary>
    protected abstract void WriteAdditionalProperties(Utf8JsonWriter writer, IDiagnosticLogger? logger);
}
