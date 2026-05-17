using Sentry.Extensibility;

namespace Sentry;

/// <summary>
/// The identifier of an event in Sentry.
/// </summary>
public readonly struct SentryId : IEquatable<SentryId>, ISentryJsonSerializable
{
    private readonly Guid _guid;

    /// <summary>
    /// An empty sentry id.
    /// </summary>
    public static readonly SentryId Empty = default;

    /// <summary>
    /// Creates a new instance of a Sentry Id.
    /// </summary>
    public SentryId(Guid guid) => _guid = guid;

    /// <summary>
    /// Sentry Id in the format Sentry recognizes.
    /// </summary>
    /// <remarks>
    /// Default <see cref="ToString"/> of <see cref="Guid"/> includes
    /// dashes which sentry doesn't expect when searching events.
    /// </remarks>
    /// <returns>String representation of the event id.</returns>
    public override string ToString() => _guid.ToString("n");

    internal bool TryFormat(Span<char> destination)
    {
#if NETSTANDARD2_0 || NET462
        var value = ToString();
        if (destination.Length < value.Length)
        {
            return false;
        }

        value.AsSpan().CopyTo(destination);
        return true;
#else
        return _guid.TryFormat(destination, out var charsWritten, "N") && charsWritten == 32;
#endif
    }

    internal bool TryWriteBytes(Span<byte> destination)
    {
        if (destination.Length < 16)
        {
            return false;
        }

#if NET8_0_OR_GREATER
        return _guid.TryWriteBytes(destination, bigEndian: true, out var bytesWritten) && bytesWritten == 16;
#else
        var bytes = _guid.ToByteArray();
        destination[0] = bytes[3];
        destination[1] = bytes[2];
        destination[2] = bytes[1];
        destination[3] = bytes[0];
        destination[4] = bytes[5];
        destination[5] = bytes[4];
        destination[6] = bytes[7];
        destination[7] = bytes[6];
        bytes.AsSpan(8).CopyTo(destination[8..]);
        return true;
#endif
    }

    /// <inheritdoc />
    public bool Equals(SentryId other) => _guid.Equals(other._guid);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is SentryId other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => _guid.GetHashCode();

    /// <summary>
    /// Generates a new Sentry ID.
    /// </summary>
    public static SentryId Create() => new(Guid.NewGuid());

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger) => writer.WriteStringValue(ToString());

    /// <summary>
    /// Parses from string.
    /// </summary>
    public static SentryId Parse(string value) => new(Guid.Parse(value));

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryId FromJson(JsonElement json)
    {
        var id = json.GetString();

        return !string.IsNullOrWhiteSpace(id)
            ? Parse(id)
            : Empty;
    }

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator ==(SentryId left, SentryId right) => left.Equals(right);

    /// <summary>
    /// Equality operator.
    /// </summary>
    public static bool operator !=(SentryId left, SentryId right) => !(left == right);

    /// <summary>
    /// The <see cref="Guid"/> from the <see cref="SentryId"/>.
    /// </summary>
    public static implicit operator Guid(SentryId sentryId) => sentryId._guid;

    /// <summary>
    /// A <see cref="SentryId"/> from a <see cref="Guid"/>.
    /// </summary>
    public static implicit operator SentryId(Guid guid) => new(guid);
}
