using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    [DataContract]
    public class SentryValues<T>
    {
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public IEnumerable<T> Values { get; }

        public SentryValues(IEnumerable<T> values) => Values = values ?? Enumerable.Empty<T>();
    }
}
