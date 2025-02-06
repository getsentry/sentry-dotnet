using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Protocol;
using Sentry.Protocol.Metrics;

namespace Sentry;

// https://develop.sentry.dev/sdk/event-payloads/transaction
/// <summary>
/// Sentry performance transaction.
/// </summary>
public class SentryTransaction : ITransactionData, ISentryJsonSerializable
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

    /// <inheritdoc />
    public string? Origin
    {
        get => Contexts.Trace.Origin;
        private set => Contexts.Trace.Origin = value;
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
    public TransactionNameSource NameSource { get; }

    /// <inheritdoc />
    public bool? IsParentSampled { get; set; }

    /// <inheritdoc />
    public string? Platform { get; set; } = SentryConstants.Platform;

    /// <inheritdoc />
    public string? Release { get; set; }

    /// <inheritdoc />
    public string? Distribution { get; set; }

    /// <inheritdoc />
    public DateTimeOffset StartTimestamp { get; private set; } = DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateTimeOffset? EndTimestamp { get; internal set; } // internal for testing

    // Not readonly because of deserialization
    private Dictionary<string, Measurement>? _measurements;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, Measurement> Measurements => _measurements ??= new Dictionary<string, Measurement>();

    /// <inheritdoc />
    public void SetMeasurement(string name, Measurement measurement) =>
        (_measurements ??= new Dictionary<string, Measurement>())[name] = measurement;

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
        internal set
        {
            Contexts.Trace.IsSampled = value;
            SampleRate ??= value == null ? null : value.Value ? 1.0 : 0.0;
        }
    }

    /// <inheritdoc />
    public double? SampleRate { get; internal set; }

    /// <inheritdoc />
    public SentryLevel? Level { get; set; }

    private SentryRequest? _request;

    /// <inheritdoc />
    public SentryRequest Request
    {
        get => _request ??= new SentryRequest();
        set => _request = value;
    }

    private readonly SentryContexts _contexts = new();

    /// <inheritdoc />
    public SentryContexts Contexts
    {
        get => _contexts;
        set => _contexts.ReplaceWith(value);
    }

    private SentryUser? _user;

    /// <inheritdoc />
    public SentryUser User
    {
        get => _user ??= new SentryUser();
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
    private SentrySpan[] _spans = Array.Empty<SentrySpan>();
    private readonly MetricsSummary? _metricsSummary;

    /// <summary>
    /// Flat list of spans within this transaction.
    /// </summary>
    public IReadOnlyCollection<SentrySpan> Spans => _spans;

    /// <inheritdoc />
    public bool IsFinished => EndTimestamp is not null;

    internal DynamicSamplingContext? DynamicSamplingContext { get; set; }

    internal ITransactionProfiler? TransactionProfiler { get; set; }

    // This constructor is used for deserialization purposes.
    // It's required because some of the fields are mapped on 'contexts.trace'.
    // When deserializing, we don't parse those fields explicitly, but
    // instead just parse the trace context and resolve them later.
    // Hence why we need a constructor that doesn't take the operation to avoid
    // overwriting it.
    private SentryTransaction(string name, TransactionNameSource nameSource)
    {
        EventId = SentryId.Create();
        Name = name;
        NameSource = nameSource;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(string name, string operation)
        : this(name, TransactionNameSource.Custom)
    {
        SpanId = SpanId.Create();
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(string name, string operation, TransactionNameSource nameSource)
        : this(name, nameSource)
    {
        SpanId = SpanId.Create();
        TraceId = SentryId.Create();
        Operation = operation;
    }

    /// <summary>
    /// Initializes an instance of <see cref="SentryTransaction"/>.
    /// </summary>
    public SentryTransaction(ITransactionTracer tracer)
        : this(tracer.Name, tracer.NameSource)
    {
        // Contexts have to be set first because other fields use that
        Contexts = tracer.Contexts;

        ParentSpanId = tracer.ParentSpanId;
        SpanId = tracer.SpanId;
        TraceId = tracer.TraceId;
        Operation = tracer.Operation;
        Platform = tracer.Platform;
        Release = tracer.Release;
        Distribution = tracer.Distribution;
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
        _extra = tracer.Extra.ToDict();
        _tags = tracer.Tags.ToDict();

        _spans = FromTracerSpans(tracer);
        _measurements = tracer.Measurements.ToDict();

        // Some items are not on the interface, but we only ever pass in a TransactionTracer anyway.
        if (tracer is TransactionTracer transactionTracer)
        {
            SampleRate = transactionTracer.SampleRate;
            DynamicSamplingContext = transactionTracer.DynamicSamplingContext;
            TransactionProfiler = transactionTracer.TransactionProfiler;
            if (transactionTracer.HasMetrics)
            {
                _metricsSummary = new MetricsSummary(transactionTracer.MetricsSummary);
            }
        }
    }

    internal static SentrySpan[] FromTracerSpans(ITransactionTracer tracer)
    {
        // Filter sentry requests created by Sentry.OpenTelemetry.SentrySpanProcessor
        var nonSentrySpans = tracer.Spans
            .Where(s => s is not SpanTracer { IsSentryRequest: true });

        if (tracer is not IBaseTracer { IsOtelInstrumenter: true })
        {
            return nonSentrySpans.Select(s => new SentrySpan(s)).ToArray();
        }

        Dictionary<SpanId, SpanId?> reHome = new();
        var spans = nonSentrySpans.ToList();
        foreach (var value in spans.ToArray())
        {
            if (value is not SpanTracer child)
            {
                continue;
            }

            // Remove any filtered spans
            if (child.IsFiltered?.Invoke() == true)
            {
                reHome.Add(child.SpanId, child.ParentSpanId);
                spans.Remove(child);
            }
        }

        // Re-home any children of filtered spans
        foreach (var value in spans)
        {
            if (value is not SpanTracer child)
            {
                continue;
            }

            while (child.ParentSpanId.HasValue && reHome.TryGetValue(child.ParentSpanId.Value, out var newParentId))
            {
                child.ParentSpanId = newParentId;
            }
        }

        return spans.Select(s => new SentrySpan(s)).ToArray();
    }

    /// <inheritdoc />
    public void AddBreadcrumb(Breadcrumb breadcrumb) =>
        _breadcrumbs.Add(breadcrumb);

    /// <inheritdoc />
    [Obsolete("Add metadata to Contexts.Trace.SetData")]
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

    /// <summary>
    /// Redacts PII from the transaction
    /// </summary>
    internal void Redact()
    {
        Description = Description?.RedactUrl();
        foreach (var breadcrumb in Breadcrumbs)
        {
            breadcrumb.Redact();
        }

        foreach (var span in Spans)
        {
            span.Redact();
        }
    }

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

        writer.WritePropertyName("transaction_info");
        writer.WriteStartObject();
        writer.WritePropertyName("source");
        writer.WriteStringValue(NameSource.ToString().ToLowerInvariant());
        writer.WriteEndObject();

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
        writer.WriteDictionaryIfNotEmpty("measurements", _measurements, logger);
        writer.WriteSerializableIfNotNull("_metrics_summary", _metricsSummary, logger);

        writer.WriteEndObject();
    }

    /// <summary>
    /// Parses transaction from JSON.
    /// </summary>
    public static SentryTransaction FromJson(JsonElement json)
    {
        var eventId = json.GetPropertyOrNull("event_id")?.Pipe(SentryId.FromJson) ?? SentryId.Empty;
        var name = json.GetProperty("transaction").GetStringOrThrow();
        var nameSource = json.GetPropertyOrNull("transaction_info")?.GetPropertyOrNull("source")?
            .GetString()?.ParseEnum<TransactionNameSource>() ?? TransactionNameSource.Custom;
        var startTimestamp = json.GetProperty("start_timestamp").GetDateTimeOffset();
        var endTimestamp = json.GetPropertyOrNull("timestamp")?.GetDateTimeOffset();
        var level = json.GetPropertyOrNull("level")?.GetString()?.ParseEnum<SentryLevel>();
        var platform = json.GetPropertyOrNull("platform")?.GetString();
        var release = json.GetPropertyOrNull("release")?.GetString();
        var distribution = json.GetPropertyOrNull("dist")?.GetString();
        var request = json.GetPropertyOrNull("request")?.Pipe(SentryRequest.FromJson);
        var contexts = json.GetPropertyOrNull("contexts")?.Pipe(SentryContexts.FromJson) ?? new();
        var user = json.GetPropertyOrNull("user")?.Pipe(SentryUser.FromJson);
        var environment = json.GetPropertyOrNull("environment")?.GetString();
        var sdk = json.GetPropertyOrNull("sdk")?.Pipe(SdkVersion.FromJson) ?? new SdkVersion();
        var fingerprint = json.GetPropertyOrNull("fingerprint")?
            .EnumerateArray().Select(j => j.GetString()!).ToArray();
        var breadcrumbs = json.GetPropertyOrNull("breadcrumbs")?
            .EnumerateArray().Select(Breadcrumb.FromJson).ToList() ?? new();
        var extra = json.GetPropertyOrNull("extra")?
            .GetDictionaryOrNull() ?? new();
        var tags = json.GetPropertyOrNull("tags")?
            .GetStringDictionaryOrNull()?.WhereNotNullValue().ToDict() ?? new();
        var measurements = json.GetPropertyOrNull("measurements")?
            .GetDictionaryOrNull(Measurement.FromJson) ?? new();
        var spans = json.GetPropertyOrNull("spans")?
            .EnumerateArray().Select(SentrySpan.FromJson).ToArray() ?? Array.Empty<SentrySpan>();

        return new SentryTransaction(name, nameSource)
        {
            EventId = eventId,
            StartTimestamp = startTimestamp,
            EndTimestamp = endTimestamp,
            Level = level,
            Platform = platform,
            Release = release,
            Distribution = distribution,
            _request = request,
            Contexts = contexts,
            _user = user,
            Environment = environment,
            Sdk = sdk,
            _fingerprint = fingerprint,
            _breadcrumbs = breadcrumbs,
            _extra = extra,
            _tags = tags,
            _measurements = measurements,
            _spans = spans
        };
    }
}
