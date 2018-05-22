using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Information about the SDK to be sent with the SentryEvent
    /// </summary>
    /// <remarks>Requires Sentry version 8.4 or higher</remarks>
    [DataContract]
    public class SdkVersion
    {
        [DataMember(Name = "integrations", EmitDefaultValue = false)]
        internal ICollection<string> InternalIntegrations { get; private set; }

        /// <summary>
        /// SDK name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }
        /// <summary>
        /// SDK Version
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }

        /// <summary>
        /// Any integration configured with the SDK
        /// </summary>
        /// <remarks>This property is not required</remarks>
        public ICollection<string> Integrations
        {
            get => InternalIntegrations ?? (InternalIntegrations = new List<string>());
            set => InternalIntegrations = value;
        }
    }
}
