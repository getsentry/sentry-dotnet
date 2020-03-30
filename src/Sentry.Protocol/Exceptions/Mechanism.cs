using System.Collections.Generic;
using System.Runtime.Serialization;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Sentry Exception Mechanism
    /// </summary>
    /// <remarks>
    /// The exception mechanism is an optional field residing in the Exception Interface.
    /// It carries additional information about the way the exception was created on the target system.
    /// This includes general exception values obtained from operating system or runtime APIs, as well as mechanism-specific values.
    /// </remarks>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/mechanism/"/>
    [DataContract]
    public class Mechanism
    {
        /// <summary>
        /// Keys found inside of the Exception Dictionary to inform if the exception was handled and which mechanism tracked it
        /// </summary>
        public static readonly string HandledKey = "Sentry:Handled";

        /// <summary>
        /// Key found inside of the Exception.Data to inform if the exception which mechanism tracked it
        /// </summary>
        public static readonly string MechanismKey = "Sentry:Mechanism";

        [DataMember(Name = "data", EmitDefaultValue = false)]
        internal Dictionary<string, object> InternalData { get; private set; }

        [DataMember(Name = "meta", EmitDefaultValue = false)]
        internal Dictionary<string, object> InternalMeta { get; private set; }

        /// <summary>
        /// Required unique identifier of this mechanism determining rendering and processing of the mechanism data
        /// </summary>
        /// <remarks>
        /// The type attribute is required to send any exception mechanism attribute,
        /// even if the SDK cannot determine the specific mechanism.
        /// In this case, set the type to "generic". See below for an example.
        /// </remarks>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; set; }

        /// <summary>
        /// Optional human readable description of the error mechanism and a possible hint on how to solve this error
        /// </summary>
        [DataMember(Name = "description", EmitDefaultValue = false)]
        public string Description { get; set; }

        /// <summary>
        /// Optional fully qualified URL to an online help resource, possible interpolated with error parameters
        /// </summary>
        [DataMember(Name = "help_link", EmitDefaultValue = false)]
        public string HelpLink { get; set; }

        /// <summary>
        /// Optional flag indicating whether the exception has been handled by the user (e.g. via try..catch)
        /// </summary>
        [DataMember(Name = "handled", EmitDefaultValue = false)]
        public bool? Handled { get; set; }

        /// <summary>
        /// Optional information from the operating system or runtime on the exception mechanism
        /// </summary>
        /// <remarks>
        /// The mechanism meta data usually carries error codes reported by the runtime or operating system,
        /// along with a platform dependent interpretation of these codes.
        /// SDKs can safely omit code names and descriptions for well known error codes, as it will be filled out by Sentry.
        /// For proprietary or vendor-specific error codes, adding these values will give additional information to the user.
        /// </remarks>
        /// <see href="https://docs.sentry.io/clientdev/interfaces/mechanism/#meta-information"/>
        public IDictionary<string, object> Meta => InternalMeta ?? (InternalMeta = new Dictionary<string, object>());

        /// <summary>
        /// Arbitrary extra data that might help the user understand the error thrown by this mechanism
        /// </summary>
        public IDictionary<string, object> Data => InternalData ?? (InternalData = new Dictionary<string, object>());
    }
}
