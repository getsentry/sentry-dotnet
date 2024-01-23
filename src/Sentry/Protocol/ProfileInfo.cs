using Sentry.Extensibility;
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

    public DebugMeta DebugMeta { get; set; } = new() { Images = new() };

    private readonly Contexts _contexts = new();

    /// <inheritdoc />
    public Contexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    public SampleProfile Profile { get; set; } = new();

    public DateTimeOffset StartTimestamp { get; set; } = DateTimeOffset.UtcNow;

    public string? Environment { get; set; }

    public string? Platform { get; set; } = Constants.Platform;

    public string? Release { get; set; }

    public SentryTransaction? Transaction { get; set; }


    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        // NOTE: some values are required by the protocol so we pass "" if we don't have them.

        writer.WriteStartObject();

        writer.WriteString("version", "1");
        writer.WriteSerializable("event_id", EventId, logger);
        writer.WriteString("timestamp", StartTimestamp);
        writer.WriteStringIfNotWhiteSpace("platform", Platform);
        writer.WriteStringIfNotWhiteSpace("release", Release);
        writer.WriteStringIfNotWhiteSpace("environment", Environment);
        if (DebugMeta.Images?.Count > 0)
        {
            writer.WriteSerializable("debug_meta", DebugMeta, logger);
        }

        // TODO writer.WriteSerializable("device", _contexts.Device, logger);
        //  https://github.com/getsentry/relay/blob/master/relay-profiling/src/sample.rs#L117
        writer.WriteStartObject("device");
        writer.WriteString("architecture", _contexts.Device.Architecture ?? "");
        writer.WriteStringIfNotWhiteSpace("manufacturer", _contexts.Device.Manufacturer);
        writer.WriteStringIfNotWhiteSpace("model", _contexts.Device.Model);
        writer.WriteEndObject();

        // TODO writer.WriteSerializable("os", _contexts.OperatingSystem, logger);
        //  https://github.com/getsentry/relay/blob/master/relay-profiling/src/sample.rs#L102
        var rawOS = _contexts.OperatingSystem.RawDescription?.Replace("Microsoft Windows", "Windows")?.Split(' ', 2);
        writer.WriteStartObject("os");
        writer.WriteString("name", _contexts.OperatingSystem.Name ?? rawOS?.First() ?? "");
        writer.WriteString("version", _contexts.OperatingSystem.Version ?? rawOS?.Last() ?? "");
        writer.WriteEndObject();

        writer.WriteSerializable("runtime", _contexts.Runtime, logger);

        if (Transaction is not null)
        {
            writer.WriteStartObject("transaction");
            // TODO try to find out transaction thread ID and map that to an index.
            writer.WriteString("active_thread_id", "0");
            writer.WriteSerializable("id", Transaction.EventId, logger);
            writer.WriteString("name", Transaction.Name);
            writer.WriteSerializable("trace_id", Transaction.TraceId, logger);
            writer.WriteEndObject();
        }

        writer.WriteSerializable("profile", Profile, logger);
        writer.WriteEndObject();
    }
}
