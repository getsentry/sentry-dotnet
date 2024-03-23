using Sentry.Extensibility;
using Sentry.Internal;

namespace Sentry;

/// <summary>
/// Sentry span ID.
/// </summary>
public readonly struct SpanId : IEquatable<SpanId>, ISentryJsonSerializable
{
    private static readonly char[] HexChars = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f' };
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
    public SpanId(string value) => long.TryParse(value, NumberStyles.HexNumber, null, out _value);

    /// <summary>
    /// Creates a new instance of a Sentry span Id.
    /// </summary>
    /// <param name="value"></param>
    public SpanId(long value) => _value = value;

    /// <inheritdoc />
    public bool Equals(SpanId other) => GetValue().Equals(other.GetValue());

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
#if NETSTANDARD2_0 || NETFRAMEWORK
        byte[] buf = new byte[8];
#else
        Span<byte> buf = stackalloc byte[8];
#endif

        Random.NextBytes(buf);

        var random = BitConverter.ToInt64(buf
#if NETSTANDARD2_0 || NETFRAMEWORK
            , 0);
#else
            );
#endif

        return new SpanId(random);
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        Span<byte> convertedBytes = stackalloc byte[sizeof(long)];
        Unsafe.As<byte, long>(ref convertedBytes[0]) = _value;

        // Going backwards through the array to preserve the order of the output hex string (i.e. `4e76` -> `76e4`)
        Span<char> output = stackalloc char[16];
        for (var i = convertedBytes.Length - 1; i >= 0; i--)
        {
            var value = convertedBytes[i];
            output[(convertedBytes.Length - 1 - i) * 2] = HexChars[value >> 4];
            output[(convertedBytes.Length - 1 - i) * 2 + 1] = HexChars[value & 0xF];
        }

        writer.WriteStringValue(output);
    }

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
