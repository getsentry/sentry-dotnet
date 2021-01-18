using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.Protocol
{
    // https://develop.sentry.dev/sdk/event-payloads/transaction
    /// <summary>
    /// Sentry performance transaction.
    /// </summary>
    public class Transaction : ITransaction, IJsonSerializable
    {
        private readonly IHub _hub;
        private readonly SpanRecorder _spanRecorder = new();

        /// <inheritdoc />
        public SentryId EventId { get; private set; }

        /// <inheritdoc />
        public SpanId SpanId
        {
            get => Contexts.Trace.SpanId;
            private set => Contexts.Trace.SpanId = value;
        }

        /// <inheritdoc />
        public SentryId TraceId
        {
            get => Contexts.Trace.TraceId;
            private set => Contexts.Trace.TraceId = value;
        }

        /// <inheritdoc />
        public string Name { get; set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; private set; }

        /// <inheritdoc />
        public string Operation
        {
            get => Contexts.Trace.Operation;
            set => Contexts.Trace.Operation = value;
        }

        /// <inheritdoc />
        public string? Description { get; set; }

        /// <inheritdoc />
        public SpanStatus? Status
        {
            get => Contexts.Trace.Status;
            set => Contexts.Trace.Status = value;
        }

        /// <inheritdoc />
        public bool IsSampled
        {
            get => Contexts.Trace.IsSampled;
            set => Contexts.Trace.IsSampled = value;
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

        private List<Breadcrumb>? _breadcrumbs;

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbs ??= new List<Breadcrumb>();

        private Dictionary<string, object?>? _extra;

        /// <inheritdoc cref="IEventLike.Extra" />
        public IReadOnlyDictionary<string, object?> Extra => _extra ??= new Dictionary<string, object?>();

        private Dictionary<string, string>? _tags;

        /// <inheritdoc cref="IEventLike.Extra" />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

        // Transaction never has a parent
        SpanId? ISpanContext.ParentSpanId => null;

        // This constructor is used for deserialization purposes.
        // It's required because some of the fields are mapped on 'contexts.trace'.
        // When deserializing, we don't parse those fields explicitly, but
        // instead just parse the trace context and resolve them later.
        // Hence why we need a constructor that doesn't take the operation.
        private Transaction(IHub hub, string name)
        {
            _hub = hub;
            EventId = SentryId.Create();
            SpanId = SpanId.Create();
            TraceId = SentryId.Create();
            Name = name;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(IHub hub, string name, string operation)
            : this(hub, name)
        {
            Operation = operation;
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
        public ISpan StartChild(string operation)
        {
            var span = new Span(_spanRecorder, SpanId, operation)
            {
                IsSampled = IsSampled
            };

            _spanRecorder.Add(span);

            return span;
        }

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.UtcNow;
            Status = status;

            // Hub may discard this transaction if `IsSampled = false`
            _hub.CaptureTransaction(this);
        }

        /// <summary>
        /// Get Sentry trace header.
        /// </summary>
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

            if (Level is {} level)
            {
                writer.WriteString("level", level.ToString().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(Name))
            {
                writer.WriteString("transaction", Name);
            }

            if (!string.IsNullOrWhiteSpace(Description))
            {
                writer.WriteString("description", Description);
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

            if (_breadcrumbs is {} breadcrumbs && breadcrumbs.Any())
            {
                writer.WriteStartArray("breadcrumbs");

                foreach (var i in breadcrumbs)
                {
                    writer.WriteSerializableValue(i);
                }

                writer.WriteEndArray();
            }

            if (_extra is {} extra && extra.Any())
            {
                writer.WriteStartObject("extra");

                foreach (var (key, value) in extra)
                {
                    writer.WriteDynamic(key, value);
                }

                writer.WriteEndObject();
            }

            if (_tags is {} tags && tags.Any())
            {
                writer.WriteStartObject("tags");

                foreach (var (key, value) in tags)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }

            writer.WriteEndObject();
        }

        /// <summary>
        /// Parses transaction from JSON.
        /// </summary>
        public static Transaction FromJson(JsonElement json)
        {
            var hub = HubAdapter.Instance;

            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var name = json.GetProperty("transaction").GetStringOrThrow();
            var description = json.GetPropertyOrNull("description")?.GetString();
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var level = json.GetPropertyOrNull("level")?.GetString()?.Pipe(s => s.ParseEnum<SentryLevel>());
            var request = json.GetPropertyOrNull("request")?.Pipe(Request.FromJson);
            var contexts = json.GetPropertyOrNull("contexts")?.Pipe(Contexts.FromJson);
            var user = json.GetPropertyOrNull("user")?.Pipe(User.FromJson);
            var environment = json.GetPropertyOrNull("environment")?.GetString();
            var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
            var fingerprint = json.GetPropertyOrNull("fingerprint")?.EnumerateArray().Select(j => j.GetString()).ToArray();
            var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?.EnumerateArray().Select(Breadcrumb.FromJson).ToList();
            var extra = json.GetPropertyOrNull("extra")?.GetObjectDictionary()?.ToDictionary();
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.ToDictionary();

            return new Transaction(hub, name)
            {
                EventId = eventId,
                Description = description,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Level = level,
                _request = request,
                _contexts = contexts,
                _user = user,
                Environment = environment,
                Sdk = sdk,
                _fingerprint = fingerprint!,
                _breadcrumbs = breadcrumbs!,
                _extra = extra!,
                _tags = tags!
            };
        }
    }
}
