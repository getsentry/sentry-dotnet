using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Additional informations about the user
        /// </summary>
        /// <value>
        /// Additional informations about the user
        /// </value>
        [DataMember(Name = "extras", EmitDefaultValue = false)]
        public Dictionary<string, string> Extras { get; set; }

        public User Clone()
        {
            var user = new User();

            CopyTo(user);

            return user;
        }

        internal void CopyTo(User user)
        {
            if (user == null)
            {
                return;
            }

            if (user.Email == null)
            {
                user.Email = Email;
            }

            if (user.Id == null)
            {
                user.Id = Id;
            }

            if (user.Username == null)
            {
                user.Username = Username;
            }

            if (user.IpAddress == null)
            {
                user.IpAddress = IpAddress;
            }

            if (user.Extras == null)
            {
                user.Extras = Extras?.ToDictionary(entry => entry.Key,
                                                  entry => entry.Value);
            }
        }
    }
}
