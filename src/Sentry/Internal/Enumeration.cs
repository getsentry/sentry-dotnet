namespace Sentry.Internal
{
    internal abstract record Enumeration(string Value)
    {
        public string Value { get; } = Value;

        public override string ToString() => Value;
    }
}
