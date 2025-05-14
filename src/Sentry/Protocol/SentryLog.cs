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
    private readonly Dictionary<string, SentryAttribute> _attributes;
    private readonly SentryLogLevel _level;

    [SetsRequiredMembers]
    internal SentryLog(DateTimeOffset timestamp, SentryId traceId, SentryLogLevel level, string message)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Level = level;
        Message = message;
        _attributes = new Dictionary<string, SentryAttribute>(7);
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
    public required SentryLogLevel Level
    {
        get => _level;
        init
        {
            SentryLogLevelExtensions.ThrowIfOutOfRange(value);
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
    public ImmutableArray<object> Parameters { get; init; }

    /// <summary>
    /// The span id of the span that was active when the log was collected.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public SpanId? ParentSpanId { get; init; }

    /// <summary>
    /// Gets the attribute value associated with the specified key when of type <see cref="string"/>.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if the <see cref="SentryLog"/> contains an attribute with the specified key of type <see cref="string"/>.
    /// Otherwise <see langword="false"/>.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public bool TryGetAttribute(string key, [NotNullWhen(true)] out string? value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "string")
        {
            value = (string)attribute.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Gets the attribute value associated with the specified key when of type <see cref="bool"/>.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if the <see cref="SentryLog"/> contains an attribute with the specified key of type <see cref="bool"/>.
    /// Otherwise <see langword="false"/>.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public bool TryGetAttribute(string key, out bool value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "boolean")
        {
            value = (bool)attribute.Value;
            return true;
        }

        value = false;
        return false;
    }

    /// <summary>
    /// Gets the attribute value associated with the specified key when of type <see cref="long"/>.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if the <see cref="SentryLog"/> contains an attribute with the specified key of type <see cref="long"/>.
    /// Otherwise <see langword="false"/>.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public bool TryGetAttribute(string key, out long value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "integer")
        {
            value = (long)attribute.Value;
            return true;
        }

        value = 0L;
        return false;
    }

    /// <summary>
    /// Gets the attribute value associated with the specified key when of type <see cref="double"/>.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if the <see cref="SentryLog"/> contains an attribute with the specified key of type <see cref="double"/>.
    /// Otherwise <see langword="false"/>.
    /// </remarks>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public bool TryGetAttribute(string key, out double value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "double")
        {
            value = (double)attribute.Value;
            return true;
        }

        value = 0.0;
        return false;
    }

    /// <summary>
    /// Set a key-value pair of <see cref="string"/> data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, string value)
    {
        _attributes[key] = new SentryAttribute(value, "string");
    }

    /// <summary>
    /// Set a key-value pair of <see cref="bool"/> data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, bool value)
    {
        _attributes[key] = new SentryAttribute(value, "boolean");
    }

    /// <summary>
    /// Set a key-value pair of <see cref="long"/> data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, long value)
    {
        _attributes[key] = new SentryAttribute(value, "integer");
    }

    /// <summary>
    /// Set a key-value pair of <see cref="double"/> data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, double value)
    {
        _attributes[key] = new SentryAttribute(value, "double");
    }

    internal void SetAttributes(SentryOptions options)
    {
        var environment = options.SettingLocator.GetEnvironment();
        SetAttribute("sentry.environment", environment);

        var release = options.SettingLocator.GetRelease();
        if (release is not null)
        {
            SetAttribute("sentry.release", release);
        }

        if (ParentSpanId.HasValue)
        {
            SetAttribute("sentry.trace.parent_span_id", ParentSpanId.Value.ToString());
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

        var (severityText, severityNumber) = Level.ToSeverityTextAndOptionalSeverityNumber();
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
            SentryAttributeSerializer.WriteAttribute(writer, "sentry.message.template", Template, "string");
        }

        for (var index = 0; index < Parameters.Length; index++)
        {
            SentryAttributeSerializer.WriteAttribute(writer, $"sentry.message.parameters.{index}", Parameters[index]);
        }

        foreach (var attribute in _attributes)
        {
            SentryAttributeSerializer.WriteAttribute(writer, attribute.Key, attribute.Value);
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
