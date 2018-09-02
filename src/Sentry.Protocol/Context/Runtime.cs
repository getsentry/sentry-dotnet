using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// This describes a runtime in more detail.
    /// </summary>
    /// <remarks>
    /// Typically this context is used multiple times if multiple runtimes are involved (for instance if you have a JavaScript application running on top of JVM)
    /// </remarks>
    /// <seealso href="https://docs.sentry.io/clientdev/interfaces/contexts/"/>
    [DataContract]
    public class Runtime
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public const string Type = "runtime";
        /// <summary>
        /// The name of the runtime.
        /// </summary>
        [DataMember(Name = "name", EmitDefaultValue = false)]
        public string Name { get; set; }
        /// <summary>
        /// The version identifier of the runtime.
        /// </summary>
        [DataMember(Name = "version", EmitDefaultValue = false)]
        public string Version { get; set; }
        /// <summary>
        ///  An optional raw description that Sentry can use in an attempt to normalize Runtime info.
        /// </summary>
        /// <remarks>
        /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
        /// this field can be used to provide a raw system info (e.g: .NET Framework 4.7.1)
        /// </remarks>
        [DataMember(Name = "raw_description", EmitDefaultValue = false)]
        public string RawDescription { get; set; }
        /// <summary>
        ///  An optional build number
        /// </summary>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
        [DataMember(Name = "build", EmitDefaultValue = false)]
        public string Build { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        public Runtime Clone()
            => new Runtime
            {
                Name = Name,
                Version = Version,
                Build = Build,
                RawDescription = RawDescription
            };
    }
}
