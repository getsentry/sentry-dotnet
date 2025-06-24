using Sentry.Extensibility;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Represents the Sentry Log protocol.
/// <para>This API is experimental and it may change in the future.</para>
/// </summary>
[Experimental(DiagnosticId.ExperimentalFeature)]
public sealed class SentryLog : ISentryJsonSerializable
{
    private readonly Dictionary<string, SentryAttribute> _attributes;

    [SetsRequiredMembers]
    internal SentryLog(DateTimeOffset timestamp, SentryId traceId, SentryLogLevel level, string message)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Level = level;
        Message = message;
        // 7 is the number of built-in attributes, so we start with that.
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
    public required SentryLogLevel Level { get; init; }

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
    /// Gets the attribute value associated with the specified key.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if the <see cref="SentryLog"/> contains an attribute with the specified key and it's value is not <see langword="null"/>.
    /// Otherwise <see langword="false"/>.
    /// Supported types:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Range</description>
    ///   </listheader>
    ///   <item>
    ///     <term>string</term>
    ///     <description><see langword="string"/> and <see langword="char"/></description>
    ///   </item>
    ///   <item>
    ///     <term>boolean</term>
    ///     <description><see langword="false"/> and <see langword="true"/></description>
    ///   </item>
    ///   <item>
    ///     <term>integer</term>
    ///     <description>64-bit signed integral numeric types</description>
    ///   </item>
    ///   <item>
    ///     <term>double</term>
    ///     <description>64-bit floating-point numeric types</description>
    ///   </item>
    /// </list>
    /// Unsupported types:
    /// <list type="table">
    ///   <listheader>
    ///     <term>Type</term>
    ///     <description>Result</description>
    ///   </listheader>
    ///   <item>
    ///     <term><see langword="object"/></term>
    ///     <description><c>ToString</c> as <c>"type": "string"</c></description>
    ///   </item>
    ///   <item>
    ///     <term>Collections</term>
    ///     <description><c>ToString</c> as <c>"type": "string"</c></description>
    ///   </item>
    ///   <item>
    ///     <term><see langword="null"/></term>
    ///     <description>ignored</description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/telemetry/logs/"/>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public bool TryGetAttribute(string key, [NotNullWhen(true)] out object? value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "object" && attribute.Value is not null)
        {
            value = attribute.Value;
            return true;
        }

        value = null;
        return false;
    }

    internal bool TryGetAttribute(string key, [NotNullWhen(true)] out string? value)
    {
        if (_attributes.TryGetValue(key, out var attribute) && attribute.Type == "string" && attribute.Value is not null)
        {
            value = (string)attribute.Value;
            return true;
        }

        value = null;
        return false;
    }

    /// <summary>
    /// Set a key-value pair of data attached to the log.
    /// <para>This API is experimental and it may change in the future.</para>
    /// </summary>
    [Experimental(DiagnosticId.ExperimentalFeature)]
    public void SetAttribute(string key, object value)
    {
        _attributes[key] = new SentryAttribute(value);
    }

    private void SetAttribute(string key, string value)
    {
        _attributes[key] = new SentryAttribute(value, "string");
    }

    internal void SetDefaultAttributes(SentryOptions options)
    {
        var environment = options.SettingLocator.GetEnvironment();
        SetAttribute("sentry.environment", environment);

        var release = options.SettingLocator.GetRelease();
        if (release is not null)
        {
            SetAttribute("sentry.release", release);
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

        var (severityText, severityNumber) = Level.ToSeverityTextAndOptionalSeverityNumber(logger);
        writer.WriteString("level", severityText);

        writer.WriteString("body", Message);

        writer.WritePropertyName("trace_id");
        TraceId.WriteTo(writer, logger);

        if (severityNumber.HasValue)
        {
            writer.WriteNumber("severity_number", severityNumber.Value);
        }

        writer.WritePropertyName("attributes");
        writer.WriteStartObject();

        if (Template is not null)
        {
            SentryAttributeSerializer.WriteStringAttribute(writer, "sentry.message.template", Template);
        }

        if (!Parameters.IsDefault)
        {
            for (var index = 0; index < Parameters.Length; index++)
            {
                SentryAttributeSerializer.WriteAttribute(writer, $"sentry.message.parameter.{index}", Parameters[index], logger);
            }
        }

        foreach (var attribute in _attributes)
        {
            SentryAttributeSerializer.WriteAttribute(writer, attribute.Key, attribute.Value, logger);
        }

        if (ParentSpanId.HasValue)
        {
            writer.WritePropertyName("sentry.trace.parent_span_id");
            writer.WriteStartObject();
            writer.WritePropertyName("value");
            ParentSpanId.Value.WriteTo(writer, logger);
            writer.WriteString("type", "string");
            writer.WriteEndObject();
        }

        writer.WriteEndObject();

        writer.WriteEndObject();
        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
