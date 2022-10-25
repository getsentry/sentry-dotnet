using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;

namespace Sentry
{
    /// <summary>
    /// An event to be sent to Sentry.
    /// </summary>
    /// <seealso href="https://develop.sentry.dev/sdk/event-payloads/" />
    [DebuggerDisplay("{GetType().Name,nq}: {" + nameof(EventId) + ",nq}")]
    public sealed class SentryEvent : IEventLike, IJsonSerializable, IHasDistribution
    {
        private IDictionary<string, string>? _modules;

        /// <summary>
        /// The <see cref="System.Exception"/> used to create this event.
        /// </summary>
        /// <remarks>
        /// The information from this exception is used by the Sentry SDK
        /// to add the relevant data to the event prior to sending to Sentry.
        /// </remarks>
        public Exception? Exception { get; }

        /// <summary>
        /// The unique identifier of this event.
        /// </summary>
        /// <remarks>
        /// Hexadecimal string representing a uuid4 value.
        /// The length is exactly 32 characters (no dashes!).
        /// </remarks>
        public SentryId EventId { get; }

        /// <summary>
        /// Indicates when the event was created.
        /// </summary>
        /// <example>2018-04-03T17:41:36</example>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets the structured message that describes this event.
        /// </summary>
        /// <remarks>
        /// This helps Sentry group events together as the grouping happens
        /// on the template message instead of the result string message.
        /// </remarks>
        /// <example>
        /// SentryMessage will have a template like: 'user {0} logged in'
        /// Or structured logging template: '{user} has logged in'
        /// </example>
        public SentryMessage? Message { get; set; }

        /// <summary>
        /// Name of the logger (or source) of the event.
        /// </summary>
        public string? Logger { get; set; }

        /// <inheritdoc />
        public string? Platform { get; set; }

        /// <summary>
        /// Identifies the computer from which the event was recorded.
        /// </summary>
        public string? ServerName { get; set; }

        /// <inheritdoc />
        public string? Release { get; set; }

        /// <inheritdoc />
        public string? Distribution { get; set; }

        internal SentryValues<SentryException>? SentryExceptionValues { get; set; }

        /// <summary>
        /// The Sentry Exception interface.
        /// </summary>
        public IEnumerable<SentryException>? SentryExceptions
        {
            get => SentryExceptionValues?.Values ?? Enumerable.Empty<SentryException>();
            set => SentryExceptionValues = value != null ? new SentryValues<SentryException>(value) : null;
        }

        private SentryValues<SentryThread>? SentryThreadValues { get; set; }

        /// <summary>
        /// The Sentry Thread interface.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/threads/"/>
        public IEnumerable<SentryThread>? SentryThreads
        {
            get => SentryThreadValues?.Values ?? Enumerable.Empty<SentryThread>();
            set => SentryThreadValues = value != null ? new SentryValues<SentryThread>(value) : null;
        }

        /// <summary>
        /// The Sentry Debug Meta Images interface.
        /// </summary>
        /// <see href="https://develop.sentry.dev/sdk/event-payloads/debugmeta#debug-images"/>
        public List<DebugImage>? DebugImages { get; set; }

        /// <summary>
        /// A list of relevant modules and their versions.
        /// </summary>
        public IDictionary<string, string> Modules => _modules ??= new Dictionary<string, string>();

        /// <inheritdoc />
        public SentryLevel? Level { get; set; }

        /// <inheritdoc />
        public string? TransactionName { get; set; }

        private Request? _request;

        /// <inheritdoc />
        public Request Request
        {
            get => _request ??= new Request();
            set => _request = value;
        }

        private readonly Contexts _contexts = new();

        /// <inheritdoc />
        public Contexts Contexts
        {
            get => _contexts;
            set => _contexts.ReplaceWith(value);
        }

        private User? _user;

        /// <inheritdoc />
        public User User
        {
            get => _user ??= new User();
            set => _user = value;
        }

        /// <inheritdoc />
        public string? Environment { get; set; }

        /// <inheritdoc />
        public SdkVersion Sdk { get; internal set; } = new();

        private IReadOnlyList<string>? _fingerprint;

        /// <inheritdoc />
        public IReadOnlyList<string> Fingerprint
        {
            get => _fingerprint ?? Array.Empty<string>();
            set => _fingerprint = value;
        }

        // Default values are null so no serialization of empty objects or arrays
        private List<Breadcrumb>? _breadcrumbs;

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs ??= new List<Breadcrumb>();

        private Dictionary<string, object?>? _extra;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extra ??= new Dictionary<string, object?>();

        private Dictionary<string, string>? _tags;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

        internal bool HasException() => Exception is not null || SentryExceptions?.Any() == true;

        internal bool HasUnhandledException() =>
            (SentryExceptions?.Any(e => !(e.Mechanism?.Handled ?? true)) ?? false)
            // Before event is processed by the client and SentryExceptions created.
            // See: AppDomainUnhandledExceptionIntegration
            || Exception?.Data[Mechanism.HandledKey] is false;

        /// <summary>
        /// Creates a new instance of <see cref="T:Sentry.SentryEvent" />.
        /// </summary>
        public SentryEvent() : this(null)
        {
        }

        /// <summary>
        /// Creates a Sentry event with optional Exception details and default values like Id and Timestamp.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public SentryEvent(Exception? exception)
            : this(exception, null)
        {
        }

        internal SentryEvent(
            Exception? exception = null,
            DateTimeOffset? timestamp = null,
            SentryId eventId = default)
        {
            Exception = exception;
            Timestamp = timestamp ?? DateTimeOffset.UtcNow;
            EventId = eventId != default ? eventId : SentryId.Create();
            Platform = Constants.Platform;
        }

        /// <inheritdoc />
        public void AddBreadcrumb(Breadcrumb breadcrumb) =>
            (_breadcrumbs ??= new List<Breadcrumb>()).Add(breadcrumb);

        /// <inheritdoc />
        public void SetExtra(string key, object? value) =>
            (_extra ??= new Dictionary<string, object?>())[key] = value;

        /// <inheritdoc />
        public void SetTag(string key, string value) =>
            (_tags ??= new Dictionary<string, string>())[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) =>
            (_tags ??= new Dictionary<string, string>()).Remove(key);

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteStringDictionaryIfNotEmpty("modules", _modules!);
            writer.WriteSerializable("event_id", EventId, logger);
            writer.WriteString("timestamp", Timestamp);
            writer.WriteSerializableIfNotNull("logentry", Message, logger);
            writer.WriteStringIfNotWhiteSpace("logger", Logger);
            writer.WriteStringIfNotWhiteSpace("platform", Platform);
            writer.WriteStringIfNotWhiteSpace("server_name", ServerName);
            writer.WriteStringIfNotWhiteSpace("release", Release);
            writer.WriteStringIfNotWhiteSpace("dist", Distribution);
            writer.WriteSerializableIfNotNull("exception", SentryExceptionValues, logger);
            writer.WriteSerializableIfNotNull("threads", SentryThreadValues, logger);
            writer.WriteStringIfNotWhiteSpace("level", Level?.ToString().ToLowerInvariant());
            writer.WriteStringIfNotWhiteSpace("transaction", TransactionName);
            writer.WriteSerializableIfNotNull("request", _request, logger);
            writer.WriteSerializableIfNotNull("contexts", _contexts.NullIfEmpty(), logger);
            writer.WriteSerializableIfNotNull("user", _user, logger);
            writer.WriteStringIfNotWhiteSpace("environment", Environment);
            writer.WriteSerializable("sdk", Sdk, logger);
            writer.WriteStringArrayIfNotEmpty("fingerprint", _fingerprint);
            writer.WriteArrayIfNotEmpty("breadcrumbs", _breadcrumbs, logger);
            writer.WriteDictionaryIfNotEmpty("extra", _extra, logger);
            writer.WriteStringDictionaryIfNotEmpty("tags", _tags!);

            if (DebugImages?.Count > 0)
            {
                writer.WritePropertyName("debug_meta");
                writer.WriteStartObject();

                writer.WriteArray("images", DebugImages.ToArray(), logger);

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses from JSON.
        /// </summary>
        public static SentryEvent FromJson(JsonElement json) => FromJson(json, null);

        internal static SentryEvent FromJson(JsonElement json, Exception? exception)
        {
            var modules = json.GetPropertyOrNull("modules")?.GetStringDictionaryOrNull();
            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var timestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var message = json.GetPropertyOrNull("logentry")?.Pipe(SentryMessage.FromJson);
            var logger = json.GetPropertyOrNull("logger")?.GetString();
            var platform = json.GetPropertyOrNull("platform")?.GetString();
            var serverName = json.GetPropertyOrNull("server_name")?.GetString();
            var release = json.GetPropertyOrNull("release")?.GetString();
            var distribution = json.GetPropertyOrNull("dist")?.GetString();
            var exceptionValues = json.GetPropertyOrNull("exception")?.GetPropertyOrNull("values")?.EnumerateArray().Select(SentryException.FromJson).ToList().Pipe(v => new SentryValues<SentryException>(v));
            var threadValues = json.GetPropertyOrNull("threads")?.GetPropertyOrNull("values")?.EnumerateArray().Select(SentryThread.FromJson).ToList().Pipe(v => new SentryValues<SentryThread>(v));
            var level = json.GetPropertyOrNull("level")?.GetString()?.ParseEnum<SentryLevel>();
            var transaction = json.GetPropertyOrNull("transaction")?.GetString();
            var request = json.GetPropertyOrNull("request")?.Pipe(Request.FromJson);
            var contexts = json.GetPropertyOrNull("contexts")?.Pipe(Contexts.FromJson);
            var user = json.GetPropertyOrNull("user")?.Pipe(User.FromJson);
            var environment = json.GetPropertyOrNull("environment")?.GetString();
            var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
            var fingerprint = json.GetPropertyOrNull("fingerprint")?.EnumerateArray().Select(j => j.GetString()).ToArray();
            var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?.EnumerateArray().Select(Breadcrumb.FromJson).ToList();
            var extra = json.GetPropertyOrNull("extra")?.GetDictionaryOrNull();
            var tags = json.GetPropertyOrNull("tags")?.GetStringDictionaryOrNull();

            var debugMeta = json.GetPropertyOrNull("debug_meta");
            var images = debugMeta?.GetPropertyOrNull("images")?.EnumerateArray().Select(DebugImage.FromJson).ToList();

            return new SentryEvent(exception, timestamp, eventId)
            {
                _modules = modules?.WhereNotNullValue().ToDictionary(),
                Message = message,
                Logger = logger,
                Platform = platform,
                ServerName = serverName,
                Release = release,
                Distribution = distribution,
                SentryExceptionValues = exceptionValues,
                SentryThreadValues = threadValues,
                DebugImages = images,
                Level = level,
                TransactionName = transaction,
                _request = request,
                Contexts = contexts ?? new(),
                _user = user,
                Environment = environment,
                Sdk = sdk,
                _fingerprint = fingerprint!,
                _breadcrumbs = breadcrumbs,
                _extra = extra?.ToDictionary(),
                _tags = tags?.WhereNotNullValue().ToDictionary()
            };
        }
    }
}
