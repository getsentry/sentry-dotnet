using Sentry.Extensibility;
using Sentry.Protocol;

namespace Sentry;

/// <summary>
/// Represents a Sentry Structured Log.
/// </summary>
/// <remarks>
/// Sentry Docs: <see href="https://docs.sentry.io/product/explore/logs/"/>.
/// Sentry Developer Documentation: <see href="https://develop.sentry.dev/sdk/telemetry/logs/"/>.
/// Sentry .NET SDK Docs: <see href="https://docs.sentry.io/platforms/dotnet/logs/"/>.
/// </remarks>
[DebuggerDisplay(@"SentryLog \{ Level = {Level}, Message = '{Message}' \}")]
public sealed class SentryLog
{
    private readonly SentryAttributes _attributes;

    [SetsRequiredMembers]
    internal SentryLog(DateTimeOffset timestamp, SentryId traceId, SentryLogLevel level, string message)
    {
        Timestamp = timestamp;
        TraceId = traceId;
        Level = level;
        Message = message;
        // 7 is the number of built-in attributes, so we start with that.
        _attributes = new SentryAttributes(7);
    }

    /// <summary>
    /// The timestamp of the log.
    /// </summary>
    /// <remarks>
    /// Sent as seconds since the Unix epoch.
    /// </remarks>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// The trace id of the log.
    /// </summary>
    public required SentryId TraceId { get; init; }

    /// <summary>
    /// The severity level of the log.
    /// </summary>
    public required SentryLogLevel Level { get; init; }

    /// <summary>
    /// The formatted log message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// The parameterized template string.
    /// </summary>
    public string? Template { get; init; }

    /// <summary>
    /// The parameters to the template string.
    /// </summary>
    public ImmutableArray<KeyValuePair<string, object>> Parameters { get; init; }

    /// <summary>
    /// The span id of the span that was active when the log was collected.
    /// </summary>
    public SpanId? SpanId { get; init; }

    /// <summary>
    /// Gets the attribute value associated with the specified key.
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
    public bool TryGetAttribute(string key, [NotNullWhen(true)] out object? value) =>
        _attributes.TryGetAttribute(key, out value);

    internal bool TryGetAttribute(string key, [NotNullWhen(true)] out string? value) =>
        _attributes.TryGetAttribute(key, out value);

    /// <summary>
    /// Set a key-value pair of data attached to the log.
    /// </summary>
    public void SetAttribute(string key, object value) => _attributes.SetAttribute(key, value);

    internal void SetAttribute(string key, string value) => _attributes.SetAttribute(key, value);

    internal void SetAttribute(string key, char value) => _attributes.SetAttribute(key, value);

    internal void SetAttribute(string key, int value) => _attributes.SetAttribute(key, value);

    internal void SetDefaultAttributes(SentryOptions options, SdkVersion sdk) =>
        _attributes.SetDefaultAttributes(options, sdk);

    internal void SetOrigin(string origin) => SetAttribute("sentry.origin", origin);

    internal void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

#if NET9_0_OR_GREATER
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / (double)TimeSpan.MillisecondsPerSecond);
#else
        writer.WriteNumber("timestamp", Timestamp.ToUnixTimeMilliseconds() / 1_000.0);
#endif

        var (severityText, severityNumber) = Level.ToSeverityTextAndOptionalSeverityNumber(logger);
        writer.WriteString("level", severityText);

        writer.WriteString("body", Message);

        writer.WritePropertyName("trace_id");
        TraceId.WriteTo(writer, logger);

        if (SpanId.HasValue)
        {
            writer.WritePropertyName("span_id");
            SpanId.Value.WriteTo(writer, logger);
        }

        if (severityNumber.HasValue)
        {
            writer.WriteNumber("severity_number", severityNumber.Value);
        }

        writer.WritePropertyName("attributes");
        writer.WriteStartObject();

        // the SDK MUST NOT attach a sentry.message.template attribute if there are no parameters
        // https://develop.sentry.dev/sdk/telemetry/logs/#default-attributes
        if (Template is not null && !Parameters.IsDefaultOrEmpty)
        {
            SentryAttributeSerializer.WriteStringAttribute(writer, "sentry.message.template", Template);
        }

        if (!Parameters.IsDefault)
        {
            foreach (var parameter in Parameters)
            {
                SentryAttributeSerializer.WriteAttribute(writer, $"sentry.message.parameter.{parameter.Key}", parameter.Value, logger);
            }
        }

        foreach (var attribute in _attributes)
        {
            SentryAttributeSerializer.WriteAttribute(writer, attribute.Key, attribute.Value, logger);
        }

        writer.WriteEndObject(); // attributes

        writer.WriteEndObject();
    }
}
