using Sentry.Extensibility;

namespace Sentry.Protocol;

/// <summary>
/// Trace origin indicates what created a trace or a span.
/// </summary>
public readonly struct Origin
{
    // Prime number picked out of a hat
    private const int HashCodeBase = 523;

    private readonly string _origin;

    internal static Origin Manual => "manual";

    /// <summary>
    /// Creates a new instance of <see cref="Origin"/>.
    /// </summary>
    public Origin()
    {
        _origin = "manual";
    }

    internal Origin(string origin)
    {
        if (OriginValidator.IsValidOrigin(origin))
        {
            _origin = origin;
        }
        else
        {
            throw new ArgumentException($"Invalid origin provided: {origin}", nameof(origin));
        }
    }

    internal static Origin? TryParse(string origin)
    {
        if (OriginValidator.IsValidOrigin(origin))
        {
            return new Origin(origin);
        }
        return null;
    }

    /// <summary>
    /// The origin is of type string and consists of four parts:
    ///   {type}.{category}.{integration-name}.{integration-part}
    /// Only the first is mandatory. The parts build upon each other, meaning it is forbidden to skip one part.
    /// For example, you may send parts one and two but aren't allowed to send parts one and three without part two.
    /// </summary>
    /// <returns></returns>
    public override string ToString() => _origin;

    /// <summary>
    /// Returns the origin as a string
    /// </summary>
    public static implicit operator string(Origin origin) => origin._origin;

    /// <summary>
    /// Converts a string to an Origin
    /// </summary>
    public static implicit operator Origin(string origin) => new(origin);

    /// <inheritdoc />
    public override int GetHashCode() => HashCodeBase + _origin.GetHashCode();

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is Origin otherOrigin && otherOrigin.GetHashCode() == GetHashCode();
    }
}
