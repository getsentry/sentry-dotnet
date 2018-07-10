using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Represents Sentry's context for OS
    /// </summary>
    /// <remarks>
    /// Defines the operating system that caused the event. In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/#context-types"/>
    [DataContract]
    public class OperatingSystem
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public const string Type = "os";
        /// <summary>
        /// The name of the operating system.
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }
        /// <summary>
        /// The version of the operating system.
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }
        /// <summary>
        /// An optional raw description that Sentry can use in an attempt to normalize OS info.
        /// </summary>
        /// <remarks>
        /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
        /// this field can be used to provide a raw system info (e.g: uname)
        /// </remarks>
        [DataMember(Name = "raw_description", EmitDefaultValue = false)]
        public string RawDescription { get; set; }
        /// <summary>
        /// The internal build revision of the operating system.
        /// </summary>
        [DataMember(Name = "build", EmitDefaultValue = false)]
        public string Build { get; set; }
        /// <summary>
        ///  If known, this can be an independent kernel version string. Typically
        /// this is something like the entire output of the 'uname' tool.
        /// </summary>
        [DataMember(Name = "kernel_version", EmitDefaultValue = false)]
        public string KernelVersion { get; set; }
        /// <summary>
        ///  An optional boolean that defines if the OS has been jailbroken or rooted.
        /// </summary>
        [DataMember(Name = "rooted", EmitDefaultValue = false)]
        public bool? Rooted { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        internal OperatingSystem Clone()
            => new OperatingSystem
            {
                Name = Name,
                Version = Version,
                RawDescription = RawDescription,
                Build = Build,
                KernelVersion = KernelVersion,
                Rooted = Rooted
            };
    }
}
