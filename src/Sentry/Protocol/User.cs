using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// An interface which describes the authenticated User for a request.
    /// </summary>
    /// <see href="https://docs.sentry.io/clientdev/interfaces/user/"/>
    [DataContract]
    public sealed class User
    {
        public static User Empty = new User();

        /// <summary>
        /// The email address of the user.
        /// </summary>
        /// <value>
        /// The user's email address.
        /// </value>
        [DataMember(Name = "email", EmitDefaultValue = false)]
        public string Email { get; }

        /// <summary>
        /// The unique ID of the user.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        [DataMember(Name = "id", EmitDefaultValue = false)]
        public string Id { get; }

        /// <summary>
        /// The IP of the user.
        /// </summary>
        /// <value>
        /// The user's IP address.
        /// </value>
        [DataMember(Name = "ip_address", EmitDefaultValue = false)]
        public string IpAddress { get; }

        /// <summary>
        /// The username of the user
        /// </summary>
        /// <value>
        /// The user's username.
        /// </value>
        [DataMember(Name = "username", EmitDefaultValue = false)]
        public string Username { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="User"/> class.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="ipAddress">The ip address.</param>
        /// <param name="username">The username.</param>
        public User(
            string email = null,
            string id = null,
            string ipAddress = null,
            string username = null)
        {
            Email = email;
            Id = id;
            IpAddress = ipAddress;
            Username = username;
        }
    }
}
