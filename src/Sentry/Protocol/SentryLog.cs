using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Sentry.Protocol;

[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLog : ISentryJsonSerializable
{
    private Dictionary<string, ValueTypePair>? _attributes;
    private int _severityNumber = -1;

    [SetsRequiredMembers]
    internal SentryLog(DateTimeOffset timestamp, SentryId traceId, SentrySeverity level, string message)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Level = level;
        Message = message;
    }

    public required DateTimeOffset Timestamp { get; init; }

    public required SentryId TraceId { get; init; }

    private SentrySeverity Level
    {
        get => SentrySeverityExtensions.FromSeverityNumber(_severityNumber);
        set => _severityNumber = SentrySeverityExtensions.ToSeverityNumber(value);
    }

    public required string Message { get; init; }

    public IReadOnlyDictionary<string, object> Attributes
    {
        get
        {
            return _attributes is null
                ? []
                : _attributes.ToDictionary(static item => item.Key, item => item.Value.Value);
        }
    }

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

    internal void SetAttributes(IHub hub, IInternalScopeManager? scopeManager, SentryOptions options)
    {
        var environment = options.SettingLocator.GetEnvironment();
        SetAttribute("sentry.environment", environment);

        var release = options.SettingLocator.GetRelease();
        if (release is not null)
        {
            SetAttribute("sentry.release", release);
        }

        if (hub.GetSpan() is { } span && span.ParentSpanId.HasValue)
        {
            SetAttribute("sentry.trace.parent_span_id", span.ParentSpanId.Value.ToString());
        }
        else if (scopeManager is not null)
        {
            var currentScope = scopeManager.GetCurrent().Key;
            var parentSpanId = currentScope.PropagationContext.ParentSpanId;
            if (parentSpanId.HasValue)
            {
                SetAttribute("sentry.trace.parent_span_id", parentSpanId.Value.ToString());
            }
        }

        SetAttribute("sentry.sdk.name", Constants.SdkName);
        if (SdkVersion.Instance.Version is { } version)
        {
            SetAttribute("sentry.sdk.version", version);
        }
    }

    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
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
            WriteAttribute(writer, "sentry.message.template", Template, "string");
        }

        if (Parameters is not null)
        {
            for (var index = 0; index < Parameters.Length; index++)
            {
                WriteAttribute(writer, $"sentry.message.parameters.{index}", Parameters[index], null);
            }
        }

        if (_attributes is not null)
        {
            foreach (var attribute in _attributes)
            {
                WriteAttribute(writer, attribute.Key, attribute.Value);
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

    private static void WriteAttribute(Utf8JsonWriter writer, string propertyName, ValueTypePair attribute)
    {
        writer.WritePropertyName(propertyName);
        if (attribute.Type is not null)
        {
            WriteAttributeValue(writer, attribute.Value, attribute.Type);
        }
        else
        {
            WriteAttributeValue(writer, attribute.Value);
        }
    }

    private static void WriteAttribute(Utf8JsonWriter writer, string propertyName, object value, string? type)
    {
        writer.WritePropertyName(propertyName);
        if (type is not null)
        {
            WriteAttributeValue(writer, value, type);
        }
        else
        {
            WriteAttributeValue(writer, value);
        }
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value, string type)
    {
        writer.WriteStartObject();

        if (type == "string")
        {
            writer.WriteString("value", (string)value);
            writer.WriteString("type", type);
        }
        else if (type == "boolean")
        {
            writer.WriteBoolean("value", (bool)value);
            writer.WriteString("type", type);
        }
        else if (type == "integer")
        {
            writer.WriteNumber("value", (int)value);
            writer.WriteString("type", type);
        }
        else if (type == "double")
        {
            writer.WriteNumber("value", (double)value);
            writer.WriteString("type", type);
        }
        else
        {
            writer.WriteString("value", value.ToString());
            writer.WriteString("type", "string");
        }

        writer.WriteEndObject();
    }

    private static void WriteAttributeValue(Utf8JsonWriter writer, object value)
    {
        writer.WriteStartObject();

        if (value is string str)
        {
            writer.WriteString("value", str);
            writer.WriteString("type", "string");
        }
        else if (value is bool boolean)
        {
            writer.WriteBoolean("value", boolean);
            writer.WriteString("type", "boolean");
        }
        else if (value is int int32)
        {
            writer.WriteNumber("value", int32);
            writer.WriteString("type", "integer");
        }
        else if (value is double float64)
        {
            writer.WriteNumber("value", float64);
            writer.WriteString("type", "double");
        }
        else
        {
            writer.WriteString("value", value.ToString());
            writer.WriteString("type", "string");
        }

        writer.WriteEndObject();
    }

    private record struct ValueTypePair(object Value, string? Type);
}
