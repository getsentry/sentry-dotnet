using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;
using Sentry.Reflection;

namespace Sentry
{
    /// <summary>
    /// Information about the SDK to be sent with the SentryEvent.
    /// </summary>
    /// <remarks>Requires Sentry version 8.4 or higher.</remarks>
    public sealed class SdkVersion : IJsonSerializable
    {
        private static readonly Lazy<SdkVersion> InstanceLazy = new(
            () => typeof(ISentryClient).Assembly.GetNameAndVersion()
        );

        internal static SdkVersion Instance => InstanceLazy.Value;

        internal ConcurrentBag<Package> InternalPackages { get; set; } = new();

        /// <summary>
        /// SDK packages.
        /// </summary>
        /// <remarks>This property is not required.</remarks>
        public IEnumerable<Package> Packages => InternalPackages;

        /// <summary>
        /// SDK name.
        /// </summary>
        public string? Name
        {
            get;
            // For integrations to set their name
            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// SDK Version.
        /// </summary>
        public string? Version
        {
            get;
            // For integrations to set their version
            [EditorBrowsable(EditorBrowsableState.Never)]
            set;
        }

        /// <summary>
        /// Add a package used to compose the SDK.
        /// </summary>
        /// <param name="name">The package name.</param>
        /// <param name="version">The package version.</param>
        public void AddPackage(string name, string version)
            => AddPackage(new Package(name, version));

        internal void AddPackage(Package package)
            => InternalPackages.Add(package);

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteArrayIfNotEmpty("packages", InternalPackages);
            writer.WriteStringIfNotWhiteSpace("name", Name);
            writer.WriteStringIfNotWhiteSpace("version", Version);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SdkVersion FromJson(JsonElement json)
        {
            // Packages
            var packages =
                json.GetPropertyOrNull("packages")?.EnumerateArray().Select(Package.FromJson).ToArray()
                ?? Array.Empty<Package>();

            // Name
            var name = json.GetPropertyOrNull("name")?.GetString() ?? "dotnet.unknown";

            // Version
            var version = json.GetPropertyOrNull("version")?.GetString() ?? "0.0.0";

            return new SdkVersion
            {
                InternalPackages = new ConcurrentBag<Package>(packages),
                Name = name,
                Version = version
            };
        }
    }
}
