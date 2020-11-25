using System.Text.Json;

namespace Sentry.Protocol
{
    /// <summary>
    /// Represents a package used to compose the SDK.
    /// </summary>
    public class Package : IJsonSerializable
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

        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Name
            writer.WriteString("name", Name);

            // Version
            writer.WriteString("version", Version);

            writer.WriteEndObject();
        }

        public static Package FromJson(JsonElement json)
        {
            var name = json.GetProperty("name").GetString() ?? "";
            var version = json.GetProperty("version").GetString() ?? "";

            return new Package(name, version);
        }
    }
}
