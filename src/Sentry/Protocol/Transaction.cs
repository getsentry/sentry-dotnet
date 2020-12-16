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
    public class Transaction : ISpan, IScope, IJsonSerializable
    {
        private readonly IHub _hub;
        private readonly SpanRecorder _spanRecorder = new();

        /// <inheritdoc />
        public IScopeOptions? ScopeOptions { get; }

        /// <summary>
        /// Transaction name.
        /// </summary>
        public string Name { get; set; } = "unnamed";

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
        public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

        /// <inheritdoc />
        public DateTimeOffset? EndTimestamp { get; private set; }

        /// <inheritdoc />
        public string Operation
        {
            get => Contexts.Trace.Operation;
            internal set => Contexts.Trace.Operation = value;
        }

        /// <inheritdoc />
        public string? Description { get; set; }

        /// <inheritdoc />
        public SpanStatus? Status
        {
            get => Contexts.Trace.Status;
            private set => Contexts.Trace.Status = value;
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

        /// <inheritdoc />
        public SdkVersion Sdk { get; internal set; } = new();

        private IEnumerable<string>? _fingerprint;

        /// <inheritdoc />
        public IEnumerable<string> Fingerprint
        {
            get => _fingerprint ?? Enumerable.Empty<string>();
            set => _fingerprint = value;
        }

        private List<Breadcrumb>? _breadcrumbs;

        /// <inheritdoc />
        public IEnumerable<Breadcrumb> Breadcrumbs => _breadcrumbs ??= new List<Breadcrumb>();

        private Dictionary<string, object?>? _extra;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, object?> Extra => _extra ??= new Dictionary<string, object?>();

        private Dictionary<string, string>? _tags;

        /// <inheritdoc />
        public IReadOnlyDictionary<string, string> Tags => _tags ??= new Dictionary<string, string>();

        // Transaction never has a parent
        SpanId? ISpanContext.ParentSpanId => null;

        string? IScope.TransactionName
        {
            get => Name;
            set => Name = value ?? "unnamed";
        }

        // TODO: this is a workaround, ideally Transaction should not inherit from IScope
        Transaction? IScope.Transaction
        {
            get => null;
            set {}
        }

        internal Transaction(IHub hub, IScopeOptions? scopeOptions)
        {
            _hub = hub;
            ScopeOptions = scopeOptions;

            SpanId = SpanId.Create();
            TraceId = SentryId.Create();
        }

        /// <inheritdoc />
        public ISpan StartChild(string operation)
        {
            var span = new Span(_spanRecorder, null, SpanId, operation);
            _spanRecorder.Add(span);

            return span;
        }

        /// <inheritdoc />
        public void Finish(SpanStatus status = SpanStatus.Ok)
        {
            EndTimestamp = DateTimeOffset.UtcNow;
            Status = status;

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
            writer.WriteSerializable("event_id", SentryId.Create());

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

            return new Transaction(hub, null)
            {
                Name = name,
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
