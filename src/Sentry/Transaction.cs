using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Internal.Extensions;

namespace Sentry
{
    // https://develop.sentry.dev/sdk/event-payloads/transaction
    /// <summary>
    /// Sentry performance transaction.
    /// </summary>
    public class Transaction : ITransactionData, IJsonSerializable
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
        public SpanId? ParentSpanId { get; private set; }

        /// <inheritdoc />
        public SentryId TraceId
        {
            get => Contexts.Trace.TraceId;
            private set => Contexts.Trace.TraceId = value;
        }

        /// <inheritdoc />
        public string Name { get; private set; }

        /// <inheritdoc />
        public string? Platform { get; set; } = Constants.Platform;

        /// <inheritdoc />
        public string? Release { get; set; }

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

        private Contexts? _contexts;

        /// <inheritdoc />
        public Contexts Contexts
        {
            get => _contexts ??= new Contexts();
            set => _contexts = value;
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
            : this(tracer.Name, tracer.Operation)
        {
            ParentSpanId = tracer.ParentSpanId;
            SpanId = tracer.SpanId;
            TraceId = tracer.TraceId;
            Platform = tracer.Platform;
            Release = tracer.Release;
            StartTimestamp = tracer.StartTimestamp;
            EndTimestamp = tracer.EndTimestamp;
            Description = tracer.Description;
            Status = tracer.Status;
            IsSampled = tracer.IsSampled;
            Level = tracer.Level;
            Request = tracer.Request;
            Contexts = tracer.Contexts;
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
            IsSampled
        );

        /// <inheritdoc />
        public void WriteTo(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WriteString("type", "transaction");
            writer.WriteSerializable("event_id", EventId);

            if (ParentSpanId is { } parentSpanId)
            {
                writer.WriteString("parent_span_id", parentSpanId);
            }

            if (Level is {} level)
            {
                writer.WriteString("level", level.ToString().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(Platform))
            {
                writer.WriteString("platform", Platform);
            }

            if (!string.IsNullOrWhiteSpace(Release))
            {
                writer.WriteString("release", Release);
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("transaction", Name);
            }

            writer.WriteString("start_timestamp", StartTimestamp);

            if (EndTimestamp is {} endTimestamp)
            {
                writer.WriteString("timestamp", endTimestamp);
            }

            if (_request is {} request)
            {
                writer.WriteSerializable("request", request);
            }

            if (_contexts is {} contexts)
            {
                writer.WriteSerializable("contexts", contexts);
            }

            if (_user is {} user)
            {
                writer.WriteSerializable("user", user);
            }

            if (!string.IsNullOrWhiteSpace(Environment))
            {
                writer.WriteString("environment", Environment);
            }

            writer.WriteSerializable("sdk", Sdk);

            if (_fingerprint is {} fingerprint && fingerprint.Any())
            {
                writer.WriteStartArray("fingerprint");

                foreach (var i in fingerprint)
                {
                    writer.WriteStringValue(i);
                }

                writer.WriteEndArray();
            }

            if (_breadcrumbs.Any())
            {
                writer.WriteStartArray("breadcrumbs");

                foreach (var i in _breadcrumbs)
                {
                    writer.WriteSerializableValue(i);
                }

                writer.WriteEndArray();
            }

            if (_extra.Any())
            {
                writer.WriteStartObject("extra");

                foreach (var (key, value) in _extra)
                {
                    writer.WriteDynamic(key, value);
                }

                writer.WriteEndObject();
            }

            if (_tags.Any())
            {
                writer.WriteStartObject("tags");

                foreach (var (key, value) in _tags)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }

            if (_spans.Any())
            {
                writer.WriteStartArray("spans");

                foreach (var span in _spans)
                {
                    writer.WriteSerializableValue(span);
                }

                writer.WriteEndArray();
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses transaction from JSON.
        /// </summary>
        public static Transaction FromJson(JsonElement json)
        {
            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var parentSpanId = json.GetPropertyOrNull("parent_span_id")?.Pipe(SpanId.FromJson);
            var name = json.GetProperty("transaction").GetStringOrThrow();
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var level = json.GetPropertyOrNull("level")?.GetString()?.Pipe(s => s.ParseEnum<SentryLevel>());
            var platform = json.GetPropertyOrNull("platform")?.GetString();
            var release = json.GetPropertyOrNull("release")?.GetString();
            var request = json.GetPropertyOrNull("request")?.Pipe(Request.FromJson);
            var contexts = json.GetPropertyOrNull("contexts")?.Pipe(Contexts.FromJson);
            var user = json.GetPropertyOrNull("user")?.Pipe(User.FromJson);
            var environment = json.GetPropertyOrNull("environment")?.GetString();
            var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
            var fingerprint = json.GetPropertyOrNull("fingerprint")?.EnumerateArray().Select(j => j.GetString()!)
                .ToArray();
            var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?.EnumerateArray().Select(Breadcrumb.FromJson)
                .Pipe(v => new List<Breadcrumb>(v));
            var extra = json.GetPropertyOrNull("extra")?.GetObjectDictionary()
                ?.ToDictionary();
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()
                ?.ToDictionary();

            var transaction = new Transaction(name)
            {
                EventId = eventId,
                ParentSpanId = parentSpanId,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Level = level,
                Platform = platform,
                Release = release,
                _request = request,
                _contexts = contexts,
                _user = user,
                Environment = environment,
                Sdk = sdk,
                _fingerprint = fingerprint,
                _breadcrumbs = breadcrumbs ?? new(),
                _extra = extra ?? new(),
                _tags = (tags ?? new())!
            };

            transaction._spans = json
                .GetPropertyOrNull("spans")?
                .EnumerateArray()
                .Select(Span.FromJson)
                .ToArray() ?? Array.Empty<Span>();

            return transaction;
        }
    }
}
