// ReSharper disable once CheckNamespace

using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// Represents Sentry's context for OS.
    /// </summary>
    /// <remarks>
    /// Defines the operating system that caused the event. In web contexts, this is the operating system of the browser (normally pulled from the User-Agent string).
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/#os-context"/>
    public sealed class OperatingSystem : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "os";

        /// <summary>
        /// The name of the operating system.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The version of the operating system.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// An optional raw description that Sentry can use in an attempt to normalize OS info.
        /// </summary>
        /// <remarks>
        /// When the system doesn't expose a clear API for <see cref="Name"/> and <see cref="Version"/>
        /// this field can be used to provide a raw system info (e.g: uname)
        /// </remarks>
        public string? RawDescription { get; set; }

        /// <summary>
        /// The internal build revision of the operating system.
        /// </summary>
        public string? Build { get; set; }

        /// <summary>
        /// If known, this can be an independent kernel version string. Typically
        /// this is something like the entire output of the 'uname' tool.
        /// </summary>
        public string? KernelVersion { get; set; }

        /// <summary>
        /// An optional boolean that defines if the OS has been jailbroken or rooted.
        /// </summary>
        public bool? Rooted { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        internal OperatingSystem Clone()
            => new()
            {
                Name = Name,
                Version = Version,
                RawDescription = RawDescription,
                Build = Build,
                KernelVersion = KernelVersion,
                Rooted = Rooted
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

            if (!string.IsNullOrWhiteSpace(KernelVersion))
            {
                writer.WriteString("kernel_version", KernelVersion);
            }

            if (Rooted is {} rooted)
            {
                writer.WriteBoolean("rooted", rooted);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static OperatingSystem FromJson(JsonElement json)
        {
            var name = json.GetPropertyOrNull("name")?.GetString();
            var version = json.GetPropertyOrNull("version")?.GetString();
            var rawDescription = json.GetPropertyOrNull("raw_description")?.GetString();
            var build = json.GetPropertyOrNull("build")?.GetString();
            var kernelVersion = json.GetPropertyOrNull("kernel_version")?.GetString();
            var rooted = json.GetPropertyOrNull("rooted")?.GetBoolean();

            return new OperatingSystem
            {
                Name = name,
                Version = version,
                RawDescription = rawDescription,
                Build = build,
                KernelVersion = kernelVersion,
                Rooted = rooted
            };
        }
    }
}
