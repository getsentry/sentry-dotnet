using Sentry.Extensibility;

namespace Sentry.Protocol;

internal class SentryAttributes : Dictionary<string, SentryAttribute>, ISentryJsonSerializable
{
    public SentryAttributes() : base(StringComparer.Ordinal)
    {
    }

    public SentryAttributes(int capacity) : base(capacity, StringComparer.Ordinal)
    {
    }

    /// <summary>
    /// Gets the attribute value associated with the specified key.
    /// </summary>
    /// <remarks>
    /// Returns <see langword="true"/> if this <see cref="SentryMetric"/> contains an attribute with the specified key which is of type <typeparamref name="TAttribute"/> and it's value is not <see langword="null"/>.
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
    /// <seealso href="https://develop.sentry.dev/sdk/telemetry/metrics/"/>
    public bool TryGetAttribute<TAttribute>(string key, [MaybeNullWhen(false)] out TAttribute value)
    {
        if (TryGetValue(key, out var attribute) && attribute.Value is TAttribute attributeValue)
        {
            value = attributeValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Set a key-value pair of data attached to the metric.
    /// </summary>
    public void SetAttribute<TAttribute>(string key, TAttribute value) where TAttribute : notnull
    {
        if (value is null)
        {
            return;
        }

        this[key] = new SentryAttribute(value);
    }

    internal void SetAttribute(string key, string value)
    {
        this[key] = new SentryAttribute(value, "string");
    }

    internal void SetAttribute(string key, char value)
    {
        this[key] = new SentryAttribute(value.ToString(), "string");
    }

    internal void SetAttribute(string key, int value)
    {
        this[key] = new SentryAttribute(value, "integer");
    }

    internal void SetDefaultAttributes(SentryOptions options, SdkVersion sdk)
    {
        var environment = options.SettingLocator.GetEnvironment();
        SetAttribute("sentry.environment", environment);

        var release = options.SettingLocator.GetRelease();
        if (release is not null)
        {
            SetAttribute("sentry.release", release);
        }

        if (sdk.Name is { } name)
        {
            SetAttribute("sentry.sdk.name", name);
        }
        if (sdk.Version is { } version)
        {
            SetAttribute("sentry.sdk.version", version);
        }
    }

    internal void SetAttributes(IEnumerable<KeyValuePair<string, object>>? attributes)
    {
        if (attributes is null)
        {
            return;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        if (attributes.TryGetNonEnumeratedCount(out var count))
        {
            _ = EnsureCapacity(Count + count);
        }
#endif

        foreach (var attribute in attributes)
        {
            this[attribute.Key] = new SentryAttribute(attribute.Value);
        }
    }

    internal void SetAttributes(ReadOnlySpan<KeyValuePair<string, object>> attributes)
    {
        if (attributes.IsEmpty)
        {
            return;
        }

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1_OR_GREATER
        _ = EnsureCapacity(Count + attributes.Length);
#endif

        foreach (var attribute in attributes)
        {
            this[attribute.Key] = new SentryAttribute(attribute.Value);
        }
    }

    /// <inheritdoc cref="ISentryJsonSerializable.WriteTo(Utf8JsonWriter, IDiagnosticLogger)" />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        foreach (var attribute in this)
        {
            SentryAttributeSerializer.WriteAttribute(writer, attribute.Key, attribute.Value, logger);
        }

        writer.WriteEndObject();
    }
}
