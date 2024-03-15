using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Describes the application.
/// </summary>
/// <remarks>
/// As opposed to the runtime, this is the actual application that
/// was running and carries meta data about the current session.
/// </remarks>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
public sealed class App : ISentryJsonSerializable, ICloneable<App>, IUpdatable<App>
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
    /// A flag indicating whether the app is in foreground or not. An app is in foreground when it's visible to the user.
    /// </summary>
    public bool? InForeground { get; set; }

    /// <summary>
    /// Clones this instance.
    /// </summary>
    internal App Clone() => ((ICloneable<App>)this).Clone();

    App ICloneable<App>.Clone()
        => new()
        {
            Identifier = Identifier,
            StartTime = StartTime,
            Hash = Hash,
            BuildType = BuildType,
            Name = Name,
            Version = Version,
            Build = Build,
            InForeground = InForeground
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(App source) => ((IUpdatable<App>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is App app)
        {
            ((IUpdatable<App>)this).UpdateFrom(app);
        }
    }

    void IUpdatable<App>.UpdateFrom(App source)
    {
        Identifier ??= source.Identifier;
        StartTime ??= source.StartTime;
        Hash ??= source.Hash;
        BuildType ??= source.BuildType;
        Name ??= source.Name;
        Version ??= source.Version;
        Build ??= source.Build;
        InForeground ??= source.InForeground;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("app_identifier", Identifier);
        writer.WriteStringIfNotNull("app_start_time", StartTime);
        writer.WriteStringIfNotWhiteSpace("device_app_hash", Hash);
        writer.WriteStringIfNotWhiteSpace("build_type", BuildType);
        writer.WriteStringIfNotWhiteSpace("app_name", Name);
        writer.WriteStringIfNotWhiteSpace("app_version", Version);
        writer.WriteStringIfNotWhiteSpace("app_build", Build);
        writer.WriteBooleanIfNotNull("in_foreground", InForeground);

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
        var inForeground = json.GetPropertyOrNull("in_foreground")?.GetBoolean();

        return new App
        {
            Identifier = identifier,
            StartTime = startTime,
            Hash = hash,
            BuildType = buildType,
            Name = name,
            Version = version,
            Build = build,
            InForeground = inForeground
        };
    }
}
