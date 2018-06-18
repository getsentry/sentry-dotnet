using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    [DataContract]
    internal class SentryValues<T>
    {
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public IEnumerable<T> Values { get; }

        public SentryValues(T value)
            => Values = value == null
            ? Enumerable.Empty<T>()
            : new[] { value };
    }
}
