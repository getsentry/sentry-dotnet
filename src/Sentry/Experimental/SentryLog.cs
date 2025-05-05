using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal.Extensions;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Sentry.Experimental;

[Experimental(DiagnosticId.ExperimentalSentryLogs)]
public sealed class SentryLog : ISentryJsonSerializable
{
    private Dictionary<string, ValueTypePair>? _attributes;
    private int _severityNumber = -1;

    [SetsRequiredMembers]
    internal SentryLog(SentrySeverity level, string message, object[]? parameters = null)
    {
        Timestamp = DateTimeOffset.UtcNow;
        TraceId = SentryId.Empty;
        Level = level;
        Message = message;
        Parameters = parameters;
    }

    public required DateTimeOffset Timestamp { get; init; }

    public required SentryId TraceId { get; init; }

    public SentrySeverity Level
    {
        get => SentrySeverityExtensions.FromSeverityNumber(_severityNumber);
        set => _severityNumber = SentrySeverityExtensions.ToSeverityNumber(value);
    }

    public required string Message { get; init; }

    //public Dictionary<string, object>? Attributes { get { return _attributes; } }

    public string? Template { get; init; }

    public object[]? Parameters { get; init; }

    public required int SeverityNumber
    {
        get => _severityNumber;
        set
        {
            SentrySeverityExtensions.ThrowIfOutOfRange(value);
            _severityNumber = value;
        }
    }

    public void SetAttribute(string key, string value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "string");
    }

    public void SetAttribute(string key, bool value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "boolean");
    }

    public void SetAttribute(string key, int value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "integer");
    }

    public void SetAttribute(string key, double value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "double");
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        _attributes = new Dictionary<string, ValueTypePair>
        {
            { "sentry.environment", new ValueTypePair("production", "string")},
            { "sentry.release", new ValueTypePair("1.0.0", "string")},
            { "sentry.trace.parent_span_id", new ValueTypePair("b0e6f15b45c36b12", "string")},
        };

        writer.WriteStartObject();
        writer.WriteStartArray("items");
        writer.WriteStartObject();

        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeSeconds());
        writer.WriteString("trace_id", TraceId);
        writer.WriteString("level", Level.ToLogString());
        writer.WriteString("body", Message);

        writer.WritePropertyName("attributes");
        writer.WriteStartObject();

        if (Template is not null)
        {
            writer.WriteSerializable("sentry.message.template", new ValueTypePair(Template, "string"), null);
        }

        if (Parameters is not null)
        {
            for (var index = 0; index < Parameters.Length; index++)
            {
                var type = "string";
                writer.WriteSerializable($"sentry.message.parameters.{index}", new ValueTypePair(Parameters[index], type), null);
            }
        }

        if (_attributes is not null)
        {
            foreach (var attribute in _attributes)
            {
                writer.WriteSerializable(attribute.Key, attribute.Value, null);
            }
        }

        writer.WriteEndObject();

        if (SeverityNumber != -1)
        {
            writer.WriteNumber("severity_number", SeverityNumber);
        }

        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
