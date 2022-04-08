using System;

namespace Sentry.Internal
{
    internal abstract record Enumeration(string Value) : IComparable
    {
        public string Value { get; } = Value;

        public override string ToString() => Value;

        public int CompareTo(object? obj) => obj is Enumeration other
            ? string.CompareOrdinal(Value, other.Value)
            : -1;
    }
}
