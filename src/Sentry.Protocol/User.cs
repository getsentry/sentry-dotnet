using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// An interface which describes the authenticated User for a request.
    /// </summary>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/user/"/>
    [DataContract]
    public class User
    {
        /// <summary>
        /// The email address of the user.
        /// </summary>
        /// <value>
        /// The user's email address.
        /// </value>
        [DataMember(Name = "email", EmitDefaultValue = false)]
        public string Email { get; set; }

        /// <summary>
        /// The unique ID of the user.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; set; }

        /// <summary>
        /// The IP of the user.
        /// </summary>
        /// <value>
        /// The user's IP address.
        /// </value>
        [DataMember(Name = "ip_address", EmitDefaultValue = false)]
        public string IpAddress { get; set; }

        /// <summary>
        /// The username of the user
        /// </summary>
        /// <value>
        /// The user's username.
        /// </value>
        [DataMember(Name = "username", EmitDefaultValue = false)]
        public string Username { get; set; }
    }
}
