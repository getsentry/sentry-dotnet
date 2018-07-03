using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel;
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
        internal IImmutableList<string> InternalIntegrations { get; set; }

        /// <summary>
        /// SDK name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name
        {
            get;
            // For integrations to set their name
            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }
        /// <summary>
        /// SDK Version
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version
        {
            get;
            // For integrations to set their version
            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// Any integration configured with the SDK
        /// </summary>
        /// <remarks>This property is not required</remarks>
        public IImmutableList<string> Integrations => InternalIntegrations ?? (InternalIntegrations = ImmutableList<string>.Empty);

        /// <summary>
        /// Adds an integration.
        /// </summary>
        /// <param name="integration">The integration.</param>
        public void AddIntegration(string integration) => InternalIntegrations = Integrations.Add(integration);

        /// <summary>
        /// Adds the integrations.
        /// </summary>
        /// <param name="integration">The integration.</param>
        public void AddIntegrations(IEnumerable<string> integration) => InternalIntegrations = Integrations.AddRange(integration);
    }
}
