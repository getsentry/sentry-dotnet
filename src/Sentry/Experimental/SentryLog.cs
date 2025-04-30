using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal.Extensions;

namespace Sentry.Experimental;

[Experimental(DiagnosticId.ExperimentalSentryLogs)]
internal sealed class SentryLog : ISentryJsonSerializable
{
    [SetsRequiredMembers]
    public SentryLog(SentrySeverity level, string message, object[]? parameters = null)
    {
        Timestamp = DateTimeOffset.UtcNow;
        TraceId = SentryId.Empty;
        Level = level;
        Message = message;
        Parameters = parameters;
    }

    public required DateTimeOffset Timestamp { get; init; }

    public required SentryId TraceId { get; init; }

    public required SentrySeverity Level { get; init; }

    public required string Message { get; init; }

    public Dictionary<string, ValueTypePair>? Attributes { get; private set; }

    public string? Template { get; init; }

    public object[]? Parameters { get; init; }

    public int SeverityNumber { get; init; } = -1;

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        Attributes = new Dictionary<string, ValueTypePair>
        {
            //{ "sentry.environment", new ValueTypePair("production", "string")},
            //{ "sentry.release", new ValueTypePair("1.0.0", "string")},
            //{ "sentry.trace.parent_span_id", new ValueTypePair("b0e6f15b45c36b12", "string")},
        };
        if (Template is not null)
        {
            Attributes["sentry.message.template"] = new ValueTypePair("User %s has logged in!", "string");
        }

        if (Parameters is not null)
        {
            for (var index = 0; index < Parameters.Length; index++)
            {
                Attributes[$"sentry.message.parameters.{index}"] = new ValueTypePair(Parameters[index], "string");
            }
        }

        writer.WriteStartObject();

        writer.WriteStartArray("items");

        writer.WriteStartObject();

        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeSeconds());
        writer.WriteString("trace_id", TraceId);
        writer.WriteString("level", Level.ToLogString());
        writer.WriteString("body", Message);
        writer.WriteDictionaryIfNotEmpty("attributes", Attributes, logger);

        if (SeverityNumber != -1)
        {
            writer.WriteNumber("severity_number", SeverityNumber);
        }

        writer.WriteEndObject();

        writer.WriteEndArray();

        writer.WriteEndObject();
    }
}

internal readonly struct ValueTypePair : ISentryJsonSerializable
{
    public ValueTypePair(object value, string type)
    {
        Value = value.ToString()!;
        Type = type;
    }

    public string Value { get; }
    public string Type { get; }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteString("value", Value);
        writer.WriteString("type", Type);

        writer.WriteEndObject();
    }
}
