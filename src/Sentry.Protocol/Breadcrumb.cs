using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;

namespace Sentry.Protocol
{
    /// <summary>
    /// Series of application events
    /// </summary>
    [DataContract]
    [DebuggerDisplay("Message: {" + nameof(Message) + "}, Type: {" + nameof(Type) + "}")]
    public sealed class Breadcrumb
    {
        [DataMember(Name = "timestamp", EmitDefaultValue = false)]
        private string SerializableTimestamp => Timestamp.ToString("yyyy-MM-ddTHH\\:mm\\:ssZ", DateTimeFormatInfo.InvariantInfo);

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
        [DataMember(Name = "message", EmitDefaultValue = false)]
        public string Message { get; }

        /// <summary>
        /// The type of breadcrumb.
        /// </summary>
        /// <remarks>
        /// The default type is default which indicates no specific handling.
        /// Other types are currently http for HTTP requests and navigation for navigation events.
        /// </remarks>
        [DataMember(Name = "type", EmitDefaultValue = false)]
        public string Type { get; }

        /// <summary>
        /// Data associated with this breadcrumb.
        /// </summary>
        /// <remarks>
        /// Contains a sub-object whose contents depend on the breadcrumb type.
        /// Additional parameters that are unsupported by the type are rendered as a key/value table.
        /// </remarks>
        [DataMember(Name = "data", EmitDefaultValue = false)]
        public IReadOnlyDictionary<string, string> Data { get; }

        /// <summary>
        /// Dotted strings that indicate what the crumb is or where it comes from.
        /// </summary>
        /// <remarks>
        /// Typically it’s a module name or a descriptive string.
        /// For instance aspnet.mvc.filter could be used to indicate that it came from an Action Filter.
        /// </remarks>
        [DataMember(Name = "category", EmitDefaultValue = false)]
        public string Category { get; }

        /// <summary>
        /// The level of the event.
        /// </summary>
        /// <remarks>
        /// Levels are used in the UI to emphasize and deemphasize the crumb.
        /// </remarks>
        [DataMember(Name = "level", EmitDefaultValue = false)]
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
            IReadOnlyDictionary<string, string> data = null,
            string category = null,
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
            string message = null,
            string type = null,
            IReadOnlyDictionary<string, string> data = null,
            string category = null,
            BreadcrumbLevel level = default)
        {
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            Message = message;
            Type = type;
            Data = data;
            Category = category;
            Level = level;
        }
    }
}
