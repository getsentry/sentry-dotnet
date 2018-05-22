using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Carries information about the browser or user agent for web-related errors.
    /// This can either be the browser this event ocurred in, or the user agent of a
    /// web request that triggered the event.
    /// </summary>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [DataContract]
    public class Browser
    {
        /// <summary>
        /// Display name of the browser application.
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }
        /// <summary>
        /// Version string of the browser.
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        internal Browser Clone()
            => new Browser
            {
                Name = Name,
                Version = Version
            };
    }
}
