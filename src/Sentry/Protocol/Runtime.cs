using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// This describes a runtime in more detail.
/// </summary>
/// <remarks>
/// Typically this context is used multiple times if multiple runtimes are involved (for instance if you have a JavaScript application running on top of JVM)
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
public sealed class Runtime : ISentryJsonSerializable, ICloneable<Runtime>, IUpdatable<Runtime>
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
    /// An optional .NET Runtime Identifier string.
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>
    /// An optional build number.
    /// </summary>
    /// <see href="https://docs.microsoft.com/en-us/dotnet/framework/migration-guide/how-to-determine-which-versions-are-installed"/>
    public string? Build { get; set; }

    /// <summary>
    /// Clones this instance
    /// </summary>
    internal Runtime Clone() => ((ICloneable<Runtime>)this).Clone();

    Runtime ICloneable<Runtime>.Clone()
        => new()
        {
            Name = Name,
            Version = Version,
            Identifier = Identifier,
            Build = Build,
            RawDescription = RawDescription
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Runtime source) => ((IUpdatable<Runtime>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Runtime runtime)
        {
            ((IUpdatable<Runtime>)this).UpdateFrom(runtime);
        }
    }

    void IUpdatable<Runtime>.UpdateFrom(Runtime source)
    {
        Name ??= source.Name;
        Version ??= source.Version;
        Identifier ??= source.Identifier;
        Build ??= source.Build;
        RawDescription ??= source.RawDescription;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("version", Version);
        writer.WriteStringIfNotWhiteSpace("raw_description", RawDescription);
        writer.WriteStringIfNotWhiteSpace("identifier", Identifier);
        writer.WriteStringIfNotWhiteSpace("build", Build);

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
        var identifier = json.GetPropertyOrNull("identifier")?.GetString();
        var build = json.GetPropertyOrNull("build")?.GetString();

        return new Runtime
        {
            Name = name,
            Version = version,
            RawDescription = rawDescription,
            Identifier = identifier,
            Build = build
        };
    }
}
