using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Represents a package used to compose the SDK.
    /// </summary>
    public sealed class Package : IJsonSerializable
    {
        /// <summary>
        /// The name of the package.
        /// </summary>
        /// <example>
        /// nuget:Sentry
        /// nuget:Sentry.AspNetCore
        /// </example>
        public string Name { get; }

        /// <summary>
        /// The version of the package.
        /// </summary>
        /// <example>
        /// 1.0.0-rc1
        /// </example>
        public string Version { get; }

        /// <summary>
        /// Creates a new instance of a <see cref="Package"/>.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version.</param>
        public Package(string name, string version)
        {
            Name = name;
            Version = version;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Name
            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("name", Name);
            }

            // Version
            if (!string.IsNullOrWhiteSpace(Version))
            {
                writer.WriteString("version", Version);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static Package FromJson(JsonElement json)
        {
            var name = json.GetProperty("name").GetStringOrThrow();
            var version = json.GetProperty("version").GetStringOrThrow();

            return new Package(name, version);
        }
    }
}
