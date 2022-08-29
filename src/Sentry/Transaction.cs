using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;

namespace Sentry
{
    // https://develop.sentry.dev/sdk/event-payloads/transaction
    /// <summary>
    /// Sentry performance transaction.
    /// </summary>
    public class Transaction : ITransactionData, IJsonSerializable, IHasDistribution
    {
        /// <summary>
        /// Transaction's event ID.
        /// </summary>
        public SentryId EventId { get; private set; }

        /// <inheritdoc />
        public SpanId SpanId
        {
            get => Contexts.Trace.SpanId;
            private set => Contexts.Trace.SpanId = value;
        }

        // A transaction normally does not have a parent because it represents
        // the top node in the span hierarchy.
        // However, a transaction may also be continued from a trace header
        // (i.e. when another service sends a request to this service),
        // in which case the newly created transaction refers to the incoming
        // transaction as the parent.

        /// <inheritdoc />
        public SpanId? ParentSpanId
        {
            get => Contexts.Trace.ParentSpanId;
            private set => Contexts.Trace.ParentSpanId = value;
        }

        /// <inheritdoc />
        public SentryId TraceId
        {
            get => Contexts.Trace.TraceId;
            private set => Contexts.Trace.TraceId = value;
        }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public bool? IsParentSampled { get; set; }

        /// <inheritdoc />
        public string? Platform { get; set; } = Constants.Platform;

        /// <inheritdoc />
        public string? Release { get; set; }

        /// <inheritdoc />
        public string? Distribution { get; set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; internal set; } // internal for testing

        /// <inheritdoc />
        public string Operation
        {
            get => Contexts.Trace.Operation;
            private set => Contexts.Trace.Operation = value;
        }

        /// <inheritdoc />
        public string? Description
        {
            get => Contexts.Trace.Description;
            set => Contexts.Trace.Description = value;
        }

        /// <inheritdoc />
        public SpanStatus? Status
        {
            get => Contexts.Trace.Status;
            private set => Contexts.Trace.Status = value;
        }

        /// <inheritdoc />
        public bool? IsSampled
        {
            get => Contexts.Trace.IsSampled;
            // Internal for unit tests
            internal set => Contexts.Trace.IsSampled = value;
        }

        /// <inheritdoc />
        public SentryLevel? Level { get; set; }

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

        // This field exists on SentryEvent and Scope, but not on Transaction
        string? IEventLike.TransactionName
        {
            get => Name;
            set => Name = value ?? "";
        }

        /// <inheritdoc />
        public SdkVersion Sdk { get; internal set; } = new();

        private IReadOnlyList<string>? _fingerprint;

        /// <inheritdoc />
        public IReadOnlyList<string> Fingerprint
        {
            get => _fingerprint ?? Array.Empty<string>();
            set => _fingerprint = value;
        }

        // Not readonly because of deserialization
        private List<Breadcrumb> _breadcrumbs = new();

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs;

        // Not readonly because of deserialization
        private Dictionary<string, object?> _extra = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extra;

        // Not readonly because of deserialization
        private Dictionary<string, string> _tags = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags;

        // Not readonly because of deserialization
        private Span[] _spans = Array.Empty<Span>();

        /// <summary>
        /// Flat list of spans within this transaction.
        /// </summary>
        public IReadOnlyCollection<Span> Spans => _spans;

        /// <inheritdoc />
        public bool IsFinished => EndTimestamp is not null;

        // This constructor is used for deserialization purposes.
        // It's required because some of the fields are mapped on 'contexts.trace'.
        // When deserializing, we don't parse those fields explicitly, but
        // instead just parse the trace context and resolve them later.
        // Hence why we need a constructor that doesn't take the operation to avoid
        // overwriting it.
        private Transaction(string name)
        {
            EventId = SentryId.Create();
            Name = name;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(string name, string operation)
            : this(name)
        {
            SpanId = SpanId.Create();
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(ITransaction tracer)
            : this(tracer.Name)
        {
            // Contexts have to be set first because other fields use that
            Contexts = tracer.Contexts;

            ParentSpanId = tracer.ParentSpanId;
            SpanId = tracer.SpanId;
            TraceId = tracer.TraceId;
            Operation = tracer.Operation;
            Platform = tracer.Platform;
            Release = tracer.Release;
            Distribution = tracer.GetDistribution();
            StartTimestamp = tracer.StartTimestamp;
            EndTimestamp = tracer.EndTimestamp;
            Description = tracer.Description;
            Status = tracer.Status;
            IsSampled = tracer.IsSampled;
            Level = tracer.Level;
            Request = tracer.Request;
            User = tracer.User;
            Environment = tracer.Environment;
            Sdk = tracer.Sdk;
            Fingerprint = tracer.Fingerprint;
            _breadcrumbs = tracer.Breadcrumbs.ToList();
            _extra = tracer.Extra.ToDictionary();
            _tags = tracer.Tags.ToDictionary();
            _spans = tracer.Spans.Select(s => new Span(s)).ToArray();
        }

        /// <inheritdoc />
        public void AddBreadcrumb(Breadcrumb breadcrumb) =>
            _breadcrumbs.Add(breadcrumb);

        /// <inheritdoc />
        public void SetExtra(string key, object? value) =>
            _extra[key] = value;

        /// <inheritdoc />
        public void SetTag(string key, string value) =>
            _tags[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) =>
            _tags.Remove(key);

        /// <inheritdoc />
        public SentryTraceHeader GetTraceHeader() => new(
            TraceId,
            SpanId,
            IsSampled);

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer, IDiagnosticLogger? logger)
        {
            writer.WriteStartObject();

            writer.WriteString("type", "transaction");
            writer.WriteSerializable("event_id", EventId, logger);
            writer.WriteStringIfNotWhiteSpace("level", Level?.ToString().ToLowerInvariant());
            writer.WriteStringIfNotWhiteSpace("platform", Platform);
            writer.WriteStringIfNotWhiteSpace("release", Release);
            writer.WriteStringIfNotWhiteSpace("dist", Distribution);
            writer.WriteStringIfNotWhiteSpace("transaction", Name);
            writer.WriteString("start_timestamp", StartTimestamp);
            writer.WriteStringIfNotNull("timestamp", EndTimestamp);
            writer.WriteSerializableIfNotNull("request", _request, logger);
            writer.WriteSerializableIfNotNull("contexts", _contexts.NullIfEmpty(), logger);
            writer.WriteSerializableIfNotNull("user", _user, logger);
            writer.WriteStringIfNotWhiteSpace("environment", Environment);
            writer.WriteSerializable("sdk", Sdk, logger);
            writer.WriteStringArrayIfNotEmpty("fingerprint", _fingerprint);
            writer.WriteArrayIfNotEmpty("breadcrumbs", _breadcrumbs, logger);
            writer.WriteDictionaryIfNotEmpty("extra", _extra, logger);
            writer.WriteStringDictionaryIfNotEmpty("tags", _tags!);
            writer.WriteArrayIfNotEmpty("spans", _spans, logger);

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses transaction from JSON.
        /// </summary>
        public static Transaction FromJson(JsonElement json)
        {
            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var name = json.GetProperty("transaction").GetStringOrThrow();
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var level = json.GetPropertyOrNull("level")?.GetString()?.ParseEnum<SentryLevel>();
            var platform = json.GetPropertyOrNull("platform")?.GetString();
            var release = json.GetPropertyOrNull("release")?.GetString();
            var distribution = json.GetPropertyOrNull("dist")?.GetString();
            var request = json.GetPropertyOrNull("request")?.Pipe(Request.FromJson);
            var contexts = json.GetPropertyOrNull("contexts")?.Pipe(Contexts.FromJson);
            var user = json.GetPropertyOrNull("user")?.Pipe(User.FromJson);
            var environment = json.GetPropertyOrNull("environment")?.GetString();
            var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
            var fingerprint = json.GetPropertyOrNull("fingerprint")?.EnumerateArray().Select(j => j.GetString()!)
                .ToArray();
            var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?.EnumerateArray().Select(Breadcrumb.FromJson)
                .Pipe(v => new List<Breadcrumb>(v));
            var extra = json.GetPropertyOrNull("extra")?.GetDictionaryOrNull()
                ?.ToDictionary();
            var tags = json.GetPropertyOrNull("tags")?.GetStringDictionaryOrNull()
                ?.ToDictionary();

            return new Transaction(name)
            {
                EventId = eventId,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Level = level,
                Platform = platform,
                Release = release,
                Distribution = distribution,
                _request = request,
                Contexts = contexts ?? new(),
                _user = user,
                Environment = environment,
                Sdk = sdk,
                _fingerprint = fingerprint,
                _breadcrumbs = breadcrumbs ?? new(),
                _extra = extra ?? new(),
                _tags = (tags ?? new())!,
                _spans = json
                    .GetPropertyOrNull("spans")?
                    .EnumerateArray()
                    .Select(Span.FromJson)
                    .ToArray() ?? Array.Empty<Span>()
            };
        }
    }
}
