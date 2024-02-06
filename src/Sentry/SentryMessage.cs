using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry;

/// <summary>
/// Sentry Message interface.
/// </summary>
/// <remarks>
/// This interface enables support to structured logging.
/// </remarks>
/// <example>
/// "sentry.interfaces.Message": {
///   "message": "Message for event: {eventId}",
///   "params": [10]
/// }
/// </example>
/// <seealso href="https://develop.sentry.dev/sdk/event-payloads/message/"/>
public sealed class SentryMessage : ISentryJsonSerializable
{
    /// <summary>
    /// The raw message string (un-interpolated).
    /// </summary>
    /// <remarks>
    /// Must be no more than 1000 characters in length.
    /// </remarks>
    public string? Message { get; set; }

    /// <summary>
    /// The optional list of formatting parameters.
    /// </summary>
    public IEnumerable<object>? Params { get; set; }

    /// <summary>
    /// The formatted message.
    /// </summary>
    public string? Formatted { get; set; }

    /// <summary>
    /// Coerces <see cref="string"/> into <see cref="SentryMessage"/>.
    /// </summary>
    public static implicit operator SentryMessage(string? message) => new() { Message = message };

    /// <inheritdoc />
    public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
    {
        writer.WriteStartObject();

        writer.WriteStringIfNotWhiteSpace("message", Message);
        writer.WriteArrayIfNotEmpty("params", Params, logger);
        writer.WriteStringIfNotWhiteSpace("formatted", Formatted);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses from JSON.
    /// </summary>
    public static SentryMessage FromJson(JsonElement json)
    {
        var message = json.GetPropertyOrNull("message")?.GetString();
        var @params = json.GetPropertyOrNull("params")?.EnumerateArray().Select(j => j.GetDynamicOrNull()).Where(o => o != null).ToArray();
        var formatted = json.GetPropertyOrNull("formatted")?.GetString();

        return new SentryMessage
        {
            Message = message,
            Params = @params!,
            Formatted = formatted
        };
    }
}
