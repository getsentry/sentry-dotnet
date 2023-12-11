namespace Sentry;

/// <summary>
/// The unit of measurement of a metric value.
/// </summary>
/// <seealso href="https://getsentry.github.io/relay/relay_metrics/enum.MetricUnit.html"/>
public readonly partial struct MeasurementUnit : IEquatable<MeasurementUnit>
{
    private readonly Enum? _unit;
    private readonly string? _name;

    private MeasurementUnit(Enum unit)
    {
        _unit = unit;
        _name = null;
    }

    private MeasurementUnit(string name)
    {
        _unit = null;
        _name = name;
    }

    /// <summary>
    /// A special measurement unit that is used for measurements that have no natural unit.
    /// </summary>
    public static MeasurementUnit None = new("none");

    /// <summary>
    /// Creates a custom measurement unit.
    /// </summary>
    /// <param name="name">The name of the custom measurement unit. It will be converted to lower case.</param>
    /// <returns>The custom measurement unit.</returns>
    public static MeasurementUnit Custom(string name) => new(name.ToLowerInvariant());

    internal static MeasurementUnit Parse(string? name)
    {
        if (name == null)
        {
            return new MeasurementUnit();
        }

        name = name.Trim();

        if (name.Length == 0)
        {
            return new MeasurementUnit();
        }

        if (name.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return None;
        }

        if (Enum.TryParse<Duration>(name, ignoreCase: true, out var duration))
        {
            return duration;
        }

        if (Enum.TryParse<Information>(name, ignoreCase: true, out var information))
        {
            return information;
        }

        if (Enum.TryParse<Fraction>(name, ignoreCase: true, out var fraction))
        {
            return fraction;
        }

        return Custom(name);
    }

    /// <summary>
    /// Returns the string representation of the measurement unit, as it will be sent to Sentry.
    /// </summary>
    public override string ToString() => _unit?.ToString().ToLowerInvariant() ?? _name ?? "";

    /// <inheritdoc />
    public bool Equals(MeasurementUnit other) => Equals(_unit, other._unit) && _name == other._name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MeasurementUnit other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_unit, _name, _unit?.GetType());

    /// <summary>
    /// Returns true if the operands are equal.
    /// </summary>
    public static bool operator ==(MeasurementUnit left, MeasurementUnit right) => left.Equals(right);

    /// <summary>
    /// Returns true if the operands are not equal.
    /// </summary>
    public static bool operator !=(MeasurementUnit left, MeasurementUnit right) => !left.Equals(right);
}
