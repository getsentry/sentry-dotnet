using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Reflection;

namespace Sentry;

/// <summary>
/// Information about the SDK to be sent with the SentryEvent.
/// </summary>
/// <remarks>Requires Sentry version 8.4 or higher.</remarks>
public sealed class SdkVersion : ISentryJsonSerializable
{
    private static readonly Lazy<SdkVersion> InstanceLazy = new(
        () => new SdkVersion
        {
            Name = "sentry.dotnet",
            Version = typeof(ISentryClient).Assembly.GetVersion()
        });

    internal static SdkVersion Instance => InstanceLazy.Value;

    internal ConcurrentBag<SentryPackage> InternalPackages { get; set; } = new();
    internal ConcurrentBag<string> Integrations { get; set; } = new();

    /// <summary>
    /// SDK packages.
    /// </summary>
    /// <remarks>This property is not required.</remarks>
    public IEnumerable<SentryPackage> Packages => InternalPackages;

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
        => AddPackage(new SentryPackage(name, version));

    internal void AddPackage(SentryPackage package)
        => InternalPackages.Add(package);

    /// <summary>
    /// Add an integration used in the SDK.
    /// </summary>
    /// <param name="integration">The integrations name.</param>
    public void AddIntegration(string integration)
        => Integrations.Add(integration);

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteArrayIfNotEmpty("packages", InternalPackages.Distinct(), logger);
        writer.WriteArrayIfNotEmpty("integrations", Integrations.Distinct(), logger);
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
            json.GetPropertyOrNull("packages")?.EnumerateArray().Select(SentryPackage.FromJson).ToArray()
            ?? Array.Empty<SentryPackage>();

        // Integrations
        var integrations =
            json.GetPropertyOrNull("integrations")?.EnumerateArray().Select(element => element.ToString() ?? "").ToArray()
            ?? Array.Empty<string>();

        // Name
        var name = json.GetPropertyOrNull("name")?.GetString() ?? "dotnet.unknown";

        // Version
        var version = json.GetPropertyOrNull("version")?.GetString() ?? "0.0.0";

        return new SdkVersion
        {
            InternalPackages = new ConcurrentBag<SentryPackage>(packages),
            Integrations = new ConcurrentBag<string>(integrations),
            Name = name,
            Version = version
        };
    }
}
