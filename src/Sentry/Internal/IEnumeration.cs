using System;

namespace Sentry.Internal
{
    internal interface IEnumeration : IComparable
    {
        internal string Value { get; }
    }

    internal interface IEnumeration<T> : IEquatable<T>, IComparable<T>, IEnumeration
    {
    }
}
