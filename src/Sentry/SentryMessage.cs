using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
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
    public sealed class SentryMessage : IJsonSerializable
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
        /// Coerces <see cref="System.String"/> into <see cref="SentryMessage"/>.
        /// </summary>
        public static implicit operator SentryMessage(string? message) => new() {Message = message};

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Message
            if (!string.IsNullOrWhiteSpace(Message))
            {
                writer.WriteString("message", Message);
            }

            // Params
            if (Params is {} @params)
            {
                writer.WriteStartArray("params");

                foreach (var i in @params)
                {
                    writer.WriteDynamicValue(i);
                }

                writer.WriteEndArray();
            }

            // Formatted
            if (!string.IsNullOrWhiteSpace(Formatted))
            {
                writer.WriteString("formatted", Formatted);
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SentryMessage FromJson(JsonElement json)
        {
            var message = json.GetPropertyOrNull("message")?.GetString();
            var @params = json.GetPropertyOrNull("params")?.EnumerateArray().Select(j => j.GetDynamic()).Where(o => o != null).ToArray();
            var formatted = json.GetPropertyOrNull("formatted")?.GetString();

            return new SentryMessage
            {
                Message = message,
                Params = @params!,
                Formatted = formatted
            };
        }
    }
}
