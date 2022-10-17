using System;

namespace Sentry.Internal
{
    internal interface IEnumeration : IComparable
    {
        internal string Value { get; }
    }
}
