namespace Sentry.Internal;

internal readonly struct DataCategory : IEnumeration<DataCategory>
{
    // See https://develop.sentry.dev/sdk/rate-limiting/#definitions for list
    public static DataCategory Attachment = new("attachment");
    public static DataCategory Default = new("default");
    public static DataCategory Error = new("error");
    public static DataCategory Internal = new("internal");
    public static DataCategory Security = new("security");
    public static DataCategory Session = new("session");
    public static DataCategory Span = new("span");
    public static DataCategory Transaction = new("transaction");
    public static DataCategory Profile = new("profile");

    private readonly string _value;

    string IEnumeration.Value => _value;

    public DataCategory(string value) => _value = value;

    public int CompareTo(DataCategory other) =>
        string.Compare(_value, other._value, StringComparison.Ordinal);

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return 1;
        }

        return obj is DataCategory other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(DataCategory)}");
    }

    public bool Equals(DataCategory other) => _value == other._value;

    public override bool Equals(object? obj) => obj is DataCategory other && Equals(other);

    public override int GetHashCode() => _value.GetHashCode();

    public override string ToString() => _value;
}
