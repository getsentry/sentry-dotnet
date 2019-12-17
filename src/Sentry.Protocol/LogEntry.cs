using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Message interface
    /// </summary>
    /// <remarks>
    /// This interface enables support to structured logging.
    /// </remarks>
    /// <example>
    /// "sentry.interfaces.Message": {
    ///   "message": "Message for event: {eventId}",
    ///   "params": [10]
    /// }
    /// </example>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/message/"/>
    [DataContract]
    public class LogEntry
    {
        /// <summary>
        /// The raw message string (uninterpolated)
        /// </summary>
        /// <remarks>
        /// Must be no more than 1000 characters in length.
        /// </remarks>
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; set; }

        /// <summary>
        /// The optional list of formatting parameters
        /// </summary>
        [DataMember(Name = "params", EmitDefaultValue = false)]
        public IEnumerable<object> Params { get; set; }

        /// <summary>
        /// The formatted message
        /// </summary>
        [DataMember(Name = "formatted", EmitDefaultValue = false)]
        public string Formatted { get; set; }
    }
}
