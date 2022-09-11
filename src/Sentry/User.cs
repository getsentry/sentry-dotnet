using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// An interface which describes the authenticated User for a request.
    /// </summary>
    /// <see href="https://develop.sentry.dev/sdk/event-payloads/user/"/>
    public sealed class User : IJsonSerializable
    {
        internal Action<User>? PropertyChanged { get; set; }

        private string? _email;

        /// <summary>
        /// The email address of the user.
        /// </summary>
        /// <value>
        /// The user's email address.
        /// </value>
        public string? Email
        {
            get => _email;
            set
            {
                _email = value;
                PropertyChanged?.Invoke(this);
            }
        }

        private string? _id;

        /// <summary>
        /// The unique ID of the user.
        /// </summary>
        /// <value>
        /// The unique identifier.
        /// </value>
        public string? Id
        {
            get => _id;
            set
            {
                _id = value;
                PropertyChanged?.Invoke(this);
            }
        }

        private string? _ipAddress;

        /// <summary>
        /// The IP of the user.
        /// </summary>
        /// <value>
        /// The user's IP address.
        /// </value>
        public string? IpAddress
        {
            get => _ipAddress;
            set
            {
                _ipAddress = value;
                PropertyChanged?.Invoke(this);
            }
        }

        private string? _username;

        /// <summary>
        /// The username of the user.
        /// </summary>
        /// <value>
        /// The user's username.
        /// </value>
        public string? Username
        {
            get => _username;
            set
            {
                _username = value;
                PropertyChanged?.Invoke(this);
            }
        }

        internal IDictionary<string, string>? InternalOther { get; private set; }

        /// <summary>
        /// Additional information about the user.
        /// </summary>
        public IDictionary<string, string> Other
        {
            get => InternalOther ??= new Dictionary<string, string>();
            set => InternalOther = value;
        }

        /// <summary>
        /// Clones the current <see cref="User"/> instance.
        /// </summary>
        /// <returns>The cloned user.</returns>
        public User Clone()
        {
            var user = new User();

            CopyTo(user);

            return user;
        }

        internal void CopyTo(User? user)
        {
            if (user == null)
            {
                return;
            }

            user.Email ??= Email;
            user.Id ??= Id;
            user.Username ??= Username;
            user.IpAddress ??= IpAddress;

            user.InternalOther ??= InternalOther?.ToDictionary(
                entry => entry.Key,
                entry => entry.Value);
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
        {
            writer.WriteStartObject();

            writer.WriteStringIfNotWhiteSpace("email", Email);
            writer.WriteStringIfNotWhiteSpace("id", Id);
            writer.WriteStringIfNotWhiteSpace("ip_address", IpAddress);
            writer.WriteStringIfNotWhiteSpace("username", Username);
            writer.WriteStringDictionaryIfNotEmpty("other", InternalOther!);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static User FromJson(JsonElement json)
        {
            var email = json.GetPropertyOrNull("email")?.GetString();
            var id = json.GetPropertyOrNull("id")?.GetString();
            var ip = json.GetPropertyOrNull("ip_address")?.GetString();
            var username = json.GetPropertyOrNull("username")?.GetString();
            var other = json.GetPropertyOrNull("other")?.GetStringDictionaryOrNull();

            return new()
            {
                Email = email,
                Id = id,
                IpAddress = ip,
                Username = username,
                InternalOther = other?.WhereNotNullValue().ToDictionary()
            };
        }
    }
}
