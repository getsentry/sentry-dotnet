using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Profiling context information.
/// </summary>
internal sealed class ProfileInfo : IJsonSerializable
{
    /// <summary>
    /// Profile's event ID.
    /// </summary>
    public SentryId EventId { get; private set; } = SentryId.Create();

    public DebugMeta DebugMeta { get; set; } = new();

    private readonly Contexts _contexts = new();

    /// <inheritdoc />
    public Contexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    public SampleProfile Profile { get; set; } = new();

    public DateTimeOffset StartTimestamp { get; set; } = new();

    public string? Environment { get; set; }

    public string? Platform { get; set; } = Constants.Platform;

    public string? Release { get; set; }

    public Transaction? Transaction { get; set; }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("version", "1");
        writer.WriteSerializable("event_id", EventId, logger);
        writer.WriteString("timestamp", StartTimestamp);
        writer.WriteStringIfNotWhiteSpace("platform", Platform);
        writer.WriteStringIfNotWhiteSpace("release", Release);
        writer.WriteStringIfNotWhiteSpace("environment", Environment);
        writer.WriteSerializable("debug_meta", DebugMeta, logger);
        writer.WriteSerializable("device", _contexts.Device, logger);
        writer.WriteSerializable("os", _contexts.OperatingSystem, logger);
        writer.WriteSerializable("runtime", _contexts.Runtime, logger);
        writer.WriteSerializable("profile", Profile, logger);

        if (Transaction is not null)
        {
            writer.WriteStartObject("transaction");
            writer.WriteString("active_thread_id", "0"); // TODO
            writer.WriteSerializable("id", Transaction.EventId, logger);
            writer.WriteString("name", Transaction.Name);
            writer.WriteSerializable("trace_id", Transaction.TraceId, logger);
            writer.WriteEndObject();
        }

        writer.WriteEndObject();
    }
}