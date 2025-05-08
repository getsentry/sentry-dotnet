using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;

namespace Sentry.Protocol;

/// <summary>
/// Represents the Sentry Log protocol.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLog : ISentryJsonSerializable
{
    private Dictionary<string, ValueTypePair>? _attributes;
    private readonly LogSeverityLevel _level;

    [SetsRequiredMembers]
    internal SentryLog(DateTimeOffset timestamp, SentryId traceId, LogSeverityLevel level, string message)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Level = level;
        Message = message;
    }

    /// <summary>
    /// The timestamp of the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Sent as seconds since the Unix epoch.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The trace id of the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public required SentryId TraceId { get; init; }

    /// <summary>
    /// The severity level of the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public required LogSeverityLevel Level
    {
        get => _level;
        init
        {
            LogSeverityLevelExtensions.ThrowIfOutOfRange(value);
            _level = value;
        }
    }

    /// <summary>
    /// The formatted log message.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public required string Message { get; init; }

    /// <summary>
    /// A dictionary of key-value pairs of arbitrary data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Attributes must also declare the type of the value.
    /// The following types are supported:
    /// <list type="bullet">
    /// <item><see cref="string"/></item>
    /// <item><see cref="bool"/></item>
    /// <item><see cref="long"/></item>
    /// <item><see cref="double"/></item>
    /// </list>
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public IReadOnlyDictionary<string, object> Attributes
    {
        get
        {
            return _attributes is null
                ? []
                : _attributes.ToDictionary(static item => item.Key, item => item.Value.Value);
        }
    }

    /// <summary>
    /// The parameterized template string.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public string? Template { get; init; }

    /// <summary>
    /// The parameters to the template string.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public object[]? Parameters { get; init; }

    /// <summary>
    /// Set a key-value pair of arbitrary data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, string value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "string");
    }

    /// <summary>
    /// Set a key-value pair of arbitrary data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, bool value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "boolean");
    }

    /// <summary>
    /// Set a key-value pair of arbitrary data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, long value)
    {
        _attributes ??= new Dictionary<string, ValueTypePair>();
        _attributes[key] = new ValueTypePair(value, "integer");
    }

    /// <summary>
    /// Set a key-value pair of arbitrary data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
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

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();
        writer.WriteStartArray("items");
        writer.WriteStartObject();

        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeSeconds());
        writer.WriteString("trace_id", TraceId);

        var (severityText, severityNumber) = Level.ToSeverityTextAndOptionalNumber();
        writer.WriteString("level", severityText);
        if (severityNumber.HasValue)
        {
            writer.WriteNumber("severity_number", severityNumber.Value);
        }

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
            writer.WriteNumber("value", (long)value);
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
        else if (value is long int64)
        {
            writer.WriteNumber("value", int64);
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
