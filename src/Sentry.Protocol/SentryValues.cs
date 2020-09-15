using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Helps serialization of Sentry protocol types which include a values property.
    /// </summary>
    [DataContract]
    public class SentryValues<T>
    {
        /// <summary>
        /// The values.
        /// </summary>
        [DataMember(Name = "values", EmitDefaultValue = false)]
        public IEnumerable<T> Values { get; }

        /// <summary>
        /// Creates an instance from the specified <see cref="IEnumerable{T}"/>.
        /// </summary>
        public SentryValues(IEnumerable<T>? values) => Values = values ?? Enumerable.Empty<T>();
    }
}
