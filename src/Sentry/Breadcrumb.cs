using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    /// <summary>
    /// Series of application events.
    /// </summary>
    [DebuggerDisplay("Message: {" + nameof(Message) + "}, Type: {" + nameof(Type) + "}")]
    public sealed class Breadcrumb : IJsonSerializable
    {
        /// <summary>
        /// A timestamp representing when the breadcrumb occurred.
        /// </summary>
        /// <remarks>
        /// This can be either an ISO datetime string, or a Unix timestamp.
        /// </remarks>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// If a message is provided, it’s rendered as text and the whitespace is preserved.
        /// Very long text might be abbreviated in the UI.
        /// </summary>
        public string? Message { get; }

        /// <summary>
        /// The type of breadcrumb.
        /// </summary>
        /// <remarks>
        /// The default type is default which indicates no specific handling.
        /// Other types are currently http for HTTP requests and navigation for navigation events.
        /// </remarks>
        public string? Type { get; }

        /// <summary>
        /// Data associated with this breadcrumb.
        /// </summary>
        /// <remarks>
        /// Contains a sub-object whose contents depend on the breadcrumb type.
        /// Additional parameters that are unsupported by the type are rendered as a key/value table.
        /// </remarks>
        public IReadOnlyDictionary<string, string>? Data { get; }

        /// <summary>
        /// Dotted strings that indicate what the crumb is or where it comes from.
        /// </summary>
        /// <remarks>
        /// Typically it’s a module name or a descriptive string.
        /// For instance aspnet.mvc.filter could be used to indicate that it came from an Action Filter.
        /// </remarks>
        public string? Category { get; }

        /// <summary>
        /// The level of the event.
        /// </summary>
        /// <remarks>
        /// Levels are used in the UI to emphasize and de-emphasize the crumb.
        /// </remarks>
        public BreadcrumbLevel Level { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Breadcrumb"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="category">The category.</param>
        /// <param name="level">The level.</param>
        public Breadcrumb(
            string message,
            string type,
            IReadOnlyDictionary<string, string>? data = null,
            string? category = null,
            BreadcrumbLevel level = default)
        : this(
            null,
            message,
            type,
            data,
            category,
            level)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Breadcrumb"/> class.
        /// </summary>
        /// <param name="timestamp"></param>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="data">The data.</param>
        /// <param name="category">The category.</param>
        /// <param name="level">The level.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        internal Breadcrumb(
            DateTimeOffset? timestamp = null,
            string? message = null,
            string? type = null,
            IReadOnlyDictionary<string, string>? data = null,
            string? category = null,
            BreadcrumbLevel level = default)
        {
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            Message = message;
            Type = type;
            Data = data;
            Category = category;
            Level = level;
        }

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            // Timestamp
            writer.WriteString(
                "timestamp",
                Timestamp.ToString("yyyy-MM-ddTHH\\:mm\\:ss.fffZ", DateTimeFormatInfo.InvariantInfo)
            );

            // Message
            if (!string.IsNullOrWhiteSpace(Message))
            {
                writer.WriteString("message", Message);
            }

            // Type
            if (!string.IsNullOrWhiteSpace(Type))
            {
                writer.WriteString("type", Type);
            }

            // Data
            if (Data is { } data)
            {
                // Why is ! required here? No idea
                writer.WriteDictionary("data", data!);
            }

            // Category
            if (!string.IsNullOrWhiteSpace(Category))
            {
                writer.WriteString("category", Category);
            }

            // Level
            if (Level != default)
            {
                writer.WriteString("level", Level.ToString().ToLowerInvariant());
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static Breadcrumb FromJson(JsonElement json)
        {
            var timestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var message = json.GetPropertyOrNull("message")?.GetString();
            var type = json.GetPropertyOrNull("type")?.GetString();
            var data = json.GetPropertyOrNull("data")?.GetDictionary();
            var category = json.GetPropertyOrNull("category")?.GetString();
            var level = json.GetPropertyOrNull("level")?.GetString()?.Pipe(s => s.ParseEnum<BreadcrumbLevel>()) ?? default;

            return new Breadcrumb(timestamp, message, type, data!, category, level);
        }
    }
}
