namespace Sentry;

/// <summary>
/// The unit of measurement of a metric value.
/// </summary>
/// <seealso href="https://getsentry.github.io/relay/relay_metrics/enum.MetricUnit.html"/>
public readonly partial struct MeasurementUnit : IEquatable<MeasurementUnit>
{
    private readonly UnitKind _kind;
    private readonly int _value;
    private readonly string? _name;

    private enum UnitKind : byte
    {
        None = 0,
        Duration = 1,
        Information = 2,
        Fraction = 3,
        Custom = 4
    }

    private static readonly string[] DurationNames = Enum.GetNames<Duration>();

    private static readonly string[] InformationNames = Enum.GetNames<Information>();

    private static readonly string[] FractionNames = Enum.GetNames<Fraction>();

    private MeasurementUnit(Duration unit)
    {
        _kind = UnitKind.Duration;
        _value = (int)unit;
        _name = null;
    }

    private MeasurementUnit(Information unit)
    {
        _kind = UnitKind.Information;
        _value = (int)unit;
        _name = null;
    }

    private MeasurementUnit(Fraction unit)
    {
        _kind = UnitKind.Fraction;
        _value = (int)unit;
        _name = null;
    }

    private MeasurementUnit(string name)
    {
        _kind = UnitKind.Custom;
        _value = default;
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

    private string? GetPredefinedName()
    {
        return _kind switch
        {
            UnitKind.Duration when (uint)_value < (uint)DurationNames.Length => DurationNames[_value],
            UnitKind.Information when (uint)_value < (uint)InformationNames.Length => InformationNames[_value],
            UnitKind.Fraction when (uint)_value < (uint)FractionNames.Length => FractionNames[_value],
            _ => null
        };
    }

    /// <summary>
    /// Returns the string representation of the measurement unit, as it will be sent to Sentry.
    /// </summary>
    public override string ToString() => ToNullableString() ?? "";

    internal string? ToNullableString() => _name ?? GetPredefinedName();

    /// <inheritdoc />
    public bool Equals(MeasurementUnit other) => _kind == other._kind && _value == other._value && _name == other._name;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MeasurementUnit other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(_kind, _value, _name);

    /// <summary>
    /// Returns true if the operands are equal.
    /// </summary>
    public static bool operator ==(MeasurementUnit left, MeasurementUnit right) => left.Equals(right);

    /// <summary>
    /// Returns true if the operands are not equal.
    /// </summary>
    public static bool operator !=(MeasurementUnit left, MeasurementUnit right) => !left.Equals(right);
}
