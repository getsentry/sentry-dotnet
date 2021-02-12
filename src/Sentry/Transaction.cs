using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry
{
    // https://develop.sentry.dev/sdk/event-payloads/transaction
    /// <summary>
    /// Sentry performance transaction.
    /// </summary>
    public class Transaction : ITransaction, IJsonSerializable
    {
        private readonly ISentryClient _client;

        /// <inheritdoc />
        public SentryId EventId { get; private set; }

        // This and many other properties in this object are mere
        // wrappers around 'contexts.trace'.
        // It poses danger as the raw contexts object is also exposed
        // to the user.
        //
        // As an example, the user can do something seemingly innocuous like this:
        //
        // var t = new Transaction(hub, "my transaction", "my op")
        // {
        //     Contexts = new Contexts
        //     {
        //         ["my ctx"] = myObj
        //     }
        // }
        //
        // The above code overwrites the contexts and, as a result, the "my op" value
        // is lost completely.

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
        public SpanId? ParentSpanId { get; }

        /// <inheritdoc />
        public SentryId TraceId
        {
            get => Contexts.Trace.TraceId;
            private set => Contexts.Trace.TraceId = value;
        }

        /// <inheritdoc cref="ITransaction.Name" />
        public string Name { get; set; }

        /// <inheritdoc />
        public string? Release { get; set; }

        /// <inheritdoc />
        public DateTimeOffset StartTimestamp { get; internal set; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; internal set; }

        /// <inheritdoc cref="ISpan.Operation" />
        public string Operation
        {
            get => Contexts.Trace.Operation;
            set => Contexts.Trace.Operation = value;
        }

        /// <inheritdoc cref="ISpan.Description" />
        public string? Description { get; set; }

        /// <inheritdoc cref="ISpan.Status" />
        public SpanStatus? Status
        {
            get => Contexts.Trace.Status;
            set => Contexts.Trace.Status = value;
        }

        /// <inheritdoc />
        public bool? IsSampled
        {
            get => Contexts.Trace.IsSampled;
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
        private Lazy<ConcurrentBag<Breadcrumb>> _breadcrumbsLazy = new();

        /// <inheritdoc />
        public IReadOnlyCollection<Breadcrumb> Breadcrumbs => _breadcrumbsLazy.Value;

        // Not readonly because of deserialization
        private Lazy<ConcurrentDictionary<string, object?>> _extraLazy = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extraLazy.Value;

        // Not readonly because of deserialization
        private Lazy<ConcurrentDictionary<string, string>> _tagsLazy = new();

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tagsLazy.Value;

        // Not readonly because of deserialization
        private Lazy<ConcurrentBag<Span>> _spansLazy = new();

        /// <inheritdoc />
        public IReadOnlyCollection<ISpan> Spans => _spansLazy.Value;

        /// <inheritdoc />
        public bool IsFinished => EndTimestamp is not null;

        // This constructor is used for deserialization purposes.
        // It's required because some of the fields are mapped on 'contexts.trace'.
        // When deserializing, we don't parse those fields explicitly, but
        // instead just parse the trace context and resolve them later.
        // Hence why we need a constructor that doesn't take the operation to avoid
        // overwriting it.
        private Transaction(
            ISentryClient client,
            string name)
        {
            _client = client;
            EventId = SentryId.Create();
            Name = name;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(ISentryClient client, string name, string operation)
            : this(client, name)
        {
            SpanId = SpanId.Create();
            TraceId = SentryId.Create();
            Operation = operation;
        }

        /// <summary>
        /// Initializes an instance of <see cref="Transaction"/>.
        /// </summary>
        public Transaction(ISentryClient client, ITransactionContext context)
            : this(client, context.Name)
        {
            SpanId = context.SpanId;
            ParentSpanId = context.ParentSpanId;
            TraceId = context.TraceId;
            Operation = context.Operation;
            Description = context.Description;
            Status = context.Status;
            IsSampled = context.IsSampled;
        }

        /// <inheritdoc />
        public void AddBreadcrumb(Breadcrumb breadcrumb) =>
            _breadcrumbsLazy.Value.Add(breadcrumb);

        /// <inheritdoc />
        public void SetExtra(string key, object? value) =>
            _extraLazy.Value[key] = value;

        /// <inheritdoc />
        public void SetTag(string key, string value) =>
            _tagsLazy.Value[key] = value;

        /// <inheritdoc />
        public void UnsetTag(string key) =>
            _tagsLazy.Value.TryRemove(key, out _);

        internal ISpan StartChild(SpanId parentSpanId, string operation)
        {
            var span = new Span(this, parentSpanId, operation)
            {
                IsSampled = IsSampled
            };

            _spansLazy.Value.Add(span);

            return span;
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation) =>
            StartChild(SpanId, operation);

        /// <inheritdoc />
        public void Finish()
        {
            EndTimestamp = DateTimeOffset.UtcNow;

            // Client decides whether to discard this transaction based on sampling
            _client.CaptureTransaction(this);
        }

        /// <inheritdoc />
        public ISpan? GetLastActiveSpan() => Spans.LastOrDefault(s => !s.IsFinished);

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

            if (Level is {} level)
            {
                writer.WriteString("level", level.ToString().ToLowerInvariant());
            }

            if (!string.IsNullOrWhiteSpace(Release))
            {
                writer.WriteString("release", Release);
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

            if (_breadcrumbsLazy.IsValueCreated && _breadcrumbsLazy.Value.Any())
            {
                writer.WriteStartArray("breadcrumbs");

                foreach (var i in _breadcrumbsLazy.Value)
                {
                    writer.WriteSerializableValue(i);
                }

                writer.WriteEndArray();
            }

            if (_extraLazy.IsValueCreated && _extraLazy.Value.Any())
            {
                writer.WriteStartObject("extra");

                foreach (var (key, value) in _extraLazy.Value)
                {
                    writer.WriteDynamic(key, value);
                }

                writer.WriteEndObject();
            }

            if (_tagsLazy.IsValueCreated && _tagsLazy.Value.Any())
            {
                writer.WriteStartObject("tags");

                foreach (var (key, value) in _tagsLazy.Value)
                {
                    writer.WriteString(key, value);
                }

                writer.WriteEndObject();
            }

            if (_spansLazy.IsValueCreated && _spansLazy.Value.Any())
            {
                writer.WriteStartArray("spans");

                foreach (var span in _spansLazy.Value)
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
            var hub = HubAdapter.Instance;

            var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
            var name = json.GetProperty("transaction").GetStringOrThrow();
            var description = json.GetPropertyOrNull("description")?.GetString();
            var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
            var endTimestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
            var level = json.GetPropertyOrNull("level")?.GetString()?.Pipe(s => s.ParseEnum<SentryLevel>());
            var release = json.GetPropertyOrNull("release")?.GetString();
            var request = json.GetPropertyOrNull("request")?.Pipe(Request.FromJson);
            var contexts = json.GetPropertyOrNull("contexts")?.Pipe(Contexts.FromJson);
            var user = json.GetPropertyOrNull("user")?.Pipe(User.FromJson);
            var environment = json.GetPropertyOrNull("environment")?.GetString();
            var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
            var fingerprint = json.GetPropertyOrNull("fingerprint")?.EnumerateArray().Select(j => j.GetString()!).ToArray();
            var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?.EnumerateArray().Select(Breadcrumb.FromJson).Pipe(v => new ConcurrentBag<Breadcrumb>(v));
            var extra = json.GetPropertyOrNull("extra")?.GetObjectDictionary()?.Pipe(v => new ConcurrentDictionary<string, object?>(v));
            var tags = json.GetPropertyOrNull("tags")?.GetDictionary()?.Pipe(v => new ConcurrentDictionary<string, string>(v!));

            var transaction = new Transaction(hub, name)
            {
                EventId = eventId,
                Description = description,
                StartTimestamp = startTimestamp,
                EndTimestamp = endTimestamp,
                Level = level,
                Release = release,
                _request = request,
                _contexts = contexts,
                _user = user,
                Environment = environment,
                Sdk = sdk,
                _fingerprint = fingerprint
            };

            if (breadcrumbs is not null)
            {
                transaction._breadcrumbsLazy = new(() => breadcrumbs);
            }

            if (extra is not null)
            {
                transaction._extraLazy = new(() => extra);
            }

            if (tags is not null)
            {
                transaction._tagsLazy = new(() => tags);
            }

            var spans = json
                .GetPropertyOrNull("spans")?
                .EnumerateArray()
                .Select(j => Span.FromJson(transaction, j))
                .Pipe(v => new ConcurrentBag<Span>(v));

            if (spans is not null)
            {
                transaction._spansLazy = new(() => spans);
            }

            return transaction;
        }
    }
}
