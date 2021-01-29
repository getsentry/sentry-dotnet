using System;
using System.Text.Json;
using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace Sentry.Protocol
{
    /// <summary>
    /// Describes the application.
    /// </summary>
    /// <remarks>
    /// As opposed to the runtime, this is the actual application that
    /// was running and carries meta data about the current session.
    /// </remarks>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
    public sealed class App : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "app";

        /// <summary>
        /// Version-independent application identifier, often a dotted bundle ID.
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// Formatted UTC timestamp when the application was started by the user.
        /// </summary>
        public DateTimeOffset? StartTime { get; set; }

        /// <summary>
        /// Application specific device identifier.
        /// </summary>
        public string? Hash { get; set; }

        /// <summary>
        /// String identifying the kind of build, e.g. testflight.
        /// </summary>
        public string? BuildType { get; set; }

        /// <summary>
        /// Human readable application name, as it appears on the platform.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Human readable application version, as it appears on the platform.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Internal build identifier, as it appears on the platform.
        /// </summary>
        public string? Build { get; set; }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        internal App Clone()
            => new()
            {
                Identifier = Identifier,
                StartTime = StartTime,
                Hash = Hash,
                BuildType = BuildType,
                Name = Name,
                Version = Version,
                Build = Build
            };

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", Type);

            if (!string.IsNullOrWhiteSpace(Identifier))
            {
                writer.WriteString("app_identifier", Identifier);
            }

            if (StartTime is {} startTime)
            {
                writer.WriteString("app_start_time", startTime);
            }

            if (!string.IsNullOrWhiteSpace(Hash))
            {
                writer.WriteString("device_app_hash", Hash);
            }

            if (!string.IsNullOrWhiteSpace(BuildType))
            {
                writer.WriteString("build_type", BuildType);
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("app_name", Name);
            }

            if (!string.IsNullOrWhiteSpace(Version))
            {
                writer.WriteString("app_version", Version);
            }

            if (!string.IsNullOrWhiteSpace(Build))
            {
                writer.WriteString("app_build", Build);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static App FromJson(JsonElement json)
        {
            var identifier = json.GetPropertyOrNull("app_identifier")?.GetString();
            var startTime = json.GetPropertyOrNull("app_start_time")?.GetDateTimeOffset();
            var hash = json.GetPropertyOrNull("device_app_hash")?.GetString();
            var buildType = json.GetPropertyOrNull("build_type")?.GetString();
            var name = json.GetPropertyOrNull("app_name")?.GetString();
            var version = json.GetPropertyOrNull("app_version")?.GetString();
            var build = json.GetPropertyOrNull("app_build")?.GetString();

            return new App
            {
                Identifier = identifier,
                StartTime = startTime,
                Hash = hash,
                BuildType = buildType,
                Name = name,
                Version = version,
                Build = build
            };
        }
    }
}
