using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol;

/// <summary>
/// Carries information about the browser or user agent for web-related errors.
/// This can either be the browser this event occurred in, or the user agent of a
/// web request that triggered the event.
/// </summary>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/contexts/"/>
public sealed class Browser : ISentryJsonSerializable, ICloneable<Browser>, IUpdatable<Browser>
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
    internal Browser Clone() => ((ICloneable<Browser>)this).Clone();

    Browser ICloneable<Browser>.Clone()
        => new()
        {
            Name = Name,
            Version = Version
        };

    /// <summary>
    /// Updates this instance with data from the properties in the <paramref name="source"/>,
    /// unless there is already a value in the existing property.
    /// </summary>
    internal void UpdateFrom(Browser source) => ((IUpdatable<Browser>)this).UpdateFrom(source);

    void IUpdatable.UpdateFrom(object source)
    {
        if (source is Browser browser)
        {
            ((IUpdatable<Browser>)this).UpdateFrom(browser);
        }
    }

    void IUpdatable<Browser>.UpdateFrom(Browser source)
    {
        Name ??= source.Name;
        Version ??= source.Version;
    }

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? _)
    {
        writer.WriteStartObject();

        writer.WriteString("type", Type);
        writer.WriteStringIfNotWhiteSpace("name", Name);
        writer.WriteStringIfNotWhiteSpace("version", Version);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static Browser FromJson(JsonElement json)
    {
        var name = json.GetPropertyOrNull("name")?.GetString();
        var version = json.GetPropertyOrNull("version")?.GetString();

        return new Browser { Name = name, Version = version };
    }
}
