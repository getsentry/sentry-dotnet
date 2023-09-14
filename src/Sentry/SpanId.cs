using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Sentry span ID.
/// </summary>
public readonly struct SpanId : IEquatable<SpanId>, IJsonSerializable
{
    private static readonly RandomValuesFactory Random = new SynchronizedRandomValuesFactory();

    private readonly long _value;

    private long GetValue() => _value;

    /// <summary>
    /// An empty Sentry span ID.
    /// </summary>
    public static readonly SpanId Empty = new(0);

    /// <summary>
    /// Creates a new instance of a Sentry span Id.
    /// </summary>
    // TODO: mark as internal in version 4
    public SpanId(string value) => long.TryParse(value, NumberStyles.HexNumber, null, out _value);

    /// <summary>
    /// Creates a new instance of a Sentry span Id.
    /// </summary>
    /// <param name="value"></param>
    public SpanId(long value) => _value = value;

    /// <inheritdoc />
    public bool Equals(SpanId other) => GetValue() == other.GetValue();

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SpanId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(_value);

    /// <inheritdoc />
    public override string ToString() => _value.ToString("x8");

    /// <summary>
    /// Generates a new Sentry ID.
    /// </summary>
    public static SpanId Create()
    {
        var buf = new byte[8];
        long random;

        do
        {
            Random.NextBytes(buf);
            random = BitConverter.ToInt64(buf, 0);
        } while (random == 0);

        return new SpanId(random);
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _) => writer.WriteStringValue(ToString());

    /// <summary>
    /// Parses from string.
    /// </summary>
    public static SpanId Parse(string value) => new(value);

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SpanId FromJson(JsonElement json)
    {
        var value = json.GetString();

        return !string.IsNullOrWhiteSpace(value)
            ? Parse(value)
            : Empty;
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(SpanId left, SpanId right) => left.Equals(right);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator !=(SpanId left, SpanId right) => !(left == right);

    /// <summary>
    /// The <see cref="Guid"/> from the <see cref="SentryId"/>.
    /// </summary>
    public static implicit operator string(SpanId id) => id.ToString();
}
