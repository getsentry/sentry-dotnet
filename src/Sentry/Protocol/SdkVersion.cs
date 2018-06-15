using System.Collections.Generic;
using System.Collections.Immutable;
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
        internal IImmutableList<string> InternalIntegrations { get; private set; }

        /// <summary>
        /// SDK name
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; internal set; }
        /// <summary>
        /// SDK Version
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; internal set; }

        // TODO: this collection should be immutable and it's Add hidden behind a method on SDK class
        /// <summary>
        /// Any integration configured with the SDK
        /// </summary>
        /// <remarks>This property is not required</remarks>
        public IImmutableList<string> Integrations
        {
            get => InternalIntegrations ?? (InternalIntegrations = ImmutableList<string>.Empty);
            private set => InternalIntegrations = value;
        }

        /// <summary>
        /// Adds an integration.
        /// </summary>
        /// <param name="integration">The integration.</param>
        public void AddIntegration(string integration) => Integrations = Integrations.Add(integration);

        /// <summary>
        /// Adds the integrations.
        /// </summary>
        /// <param name="integration">The integration.</param>
        public void AddIntegrations(IEnumerable<string> integration) => Integrations = Integrations.AddRange(integration);
    }
}
