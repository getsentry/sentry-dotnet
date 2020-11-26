// ReSharper disable once CheckNamespace

using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    /// <summary>
    /// Carries information about the browser or user agent for web-related errors.
    /// This can either be the browser this event occurred in, or the user agent of a
    /// web request that triggered the event.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
    public class Browser : IJsonSerializable
    {
        /// <summary>
        /// Tells Sentry which type of context this is.
        /// </summary>
        public const string Type = "browser";

        /// <summary>
        /// Display name of the browser application.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Version string of the browser.
        /// </summary>
        public string? Version { get; set; }

        /// <summary>
        /// Clones this instance
        /// </summary>
        internal Browser Clone()
            => new Browser
            {
                Name = Name,
                Version = Version
            };

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

            writer.WriteEndObject();
        }

        public static Browser FromJson(JsonElement json)
        {
            var name = json.GetPropertyOrNull("name")?.GetString();
            var version = json.GetPropertyOrNull("version")?.GetString();

            return new Browser {Name = name, Version = version};
        }
    }
}
