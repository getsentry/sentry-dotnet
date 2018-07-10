using System;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Describes the application.
    /// </summary>
    /// <remarks>
    /// As opposed to the runtime, this is the actual application that
    /// was running and carries meta data about the current session.
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [DataContract]
    public class App
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public const string Type = "app";
        /// <summary>
        /// Version-independent application identifier, often a dotted bundle ID.
        /// </summary>
        [DataMember(Name = "app_identifier", EmitDefaultValue = false)]
        public string Identifier { get; set; }
        /// <summary>
        /// Formatted UTC timestamp when the application was started by the user.
        /// </summary>
        [DataMember(Name = "app_start_time", EmitDefaultValue = false)]
        public DateTimeOffset? StartTime { get; set; }
        /// <summary>
        /// Application specific device identifier.
        /// </summary>
        [DataMember(Name = "device_app_hash", EmitDefaultValue = false)]
        public string Hash { get; set; }
        /// <summary>
        /// String identifying the kind of build, e.g. testflight.
        /// </summary>
        [DataMember(Name = "build_type", EmitDefaultValue = false)]
        public string BuildType { get; set; }
        /// <summary>
        /// Human readable application name, as it appears on the platform.
        /// </summary>
        [DataMember(Name = "app_name", EmitDefaultValue = false)]
        public string Name { get; set; }
        /// <summary>
        /// Human readable application version, as it appears on the platform.
        /// </summary>
        [DataMember(Name = "app_version", EmitDefaultValue = false)]
        public string Version { get; set; }
        /// <summary>
        /// Internal build identifier, as it appears on the platform.
        /// </summary>
        [DataMember(Name = "app_build", EmitDefaultValue = false)]
        public string Build { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        internal App Clone()
            => new App
            {
                Identifier = Identifier,
                StartTime = StartTime,
                Hash = Hash,
                BuildType = BuildType,
                Name = Name,
                Version = Version,
                Build = Build
            };
    }
}
