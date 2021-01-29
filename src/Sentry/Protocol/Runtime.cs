// ReSharper disable once CheckNamespace

using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// This describes a runtime in more detail.
    /// </summary>
    /// <remarks>
    /// Typically this context is used multiple times if multiple runtimes are involved (for instance if you have a JavaScript application running on top of JVM)
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
    public sealed class Runtime : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "runtime";

        /// <summary>
        /// The name of the runtime.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The version identifier of the runtime.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        ///  An optional raw description that Sentry can use in an attempt to normalize Runtime info.
        /// </summary>
        /// <remarks>
        /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
        /// this field can be used to provide a raw system info (e.g: .NET Framework 4.7.1).
        /// </remarks>
        public string? RawDescription { get; set; }

        /// <summary>
        /// An optional build number.
        /// </summary>
        /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
        public string? Build { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        /// <returns></returns>
        public Runtime Clone()
            => new()
            {
                Name = Name,
                Version = Version,
                Build = Build,
                RawDescription = RawDescription
            };

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("name", Name);
            }

            if (!string.IsNullOrWhiteSpace(Version))
            {
                writer.WriteString("version", Version);
            }

            if (!string.IsNullOrWhiteSpace(RawDescription))
            {
                writer.WriteString("raw_description", RawDescription);
            }

            if (!string.IsNullOrWhiteSpace(Build))
            {
                writer.WriteString("build", Build);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static Runtime FromJson(JsonElement json)
        {
            var name = json.GetPropertyOrNull("name")?.GetString();
            var version = json.GetPropertyOrNull("version")?.GetString();
            var rawDescription = json.GetPropertyOrNull("raw_description")?.GetString();
            var build = json.GetPropertyOrNull("build")?.GetString();

            return new Runtime
            {
                Name = name,
                Version = version,
                RawDescription = rawDescription,
                Build = build
            };
        }
    }
}
