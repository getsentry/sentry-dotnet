using OpenTelemetry;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.OpenTelemetry;

// https://develop.sentry.dev/sdk/performance/opentelemetry/#step-1-implement-the-sentryspanprocessor-on-your-sdk

/// <summary>
/// Sentry span processor for Open Telemetry.
/// </summary>
public class SentrySpanProcessor : BaseProcessor<Activity>
{
    private readonly IHub _hub;

    private readonly ConcurrentDictionary<ActivitySpanId, ISpan> _map = new();
    private readonly SentryOptions? _options;
    private readonly string? _sentryBaseUrl;
    private readonly Lazy<IDictionary<string, object>> _resourceAttributes;

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor(IHub hub)
    {
        _hub = hub;
        _options = hub.GetSentryOptions();
        if (_options?.Dsn is { } dsn)
        {
            _sentryBaseUrl = new Uri(dsn).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
        }

        // Resource attributes are consistent between spans, but not available during construction.
        // Thus, get a single instance lazily.
        _resourceAttributes = new Lazy<IDictionary<string, object>>(() =>
            ParentProvider?.GetResource().Attributes.ToDictionary() ?? new Dictionary<string, object>(0));

        hub.ConfigureScope(scope =>
        {
            scope.AddTransactionProcessor(transaction =>
            {
                var activity = Activity.Current;
                if (activity != null)
                {
                    var trace = transaction.Contexts.Trace;
                    trace.TraceId = activity.TraceId.AsSentryId();
                    trace.SpanId = activity.SpanId.AsSentrySpanId();
                }

                return transaction;
            });
        });
    }

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        ISpan span;
        if (_map.TryGetValue(data.ParentSpanId, out var parentSpan))
        {
            // The parent span exists - start a child span.
            span = parentSpan.StartChild(
                data.SpanId.AsSentrySpanId(),
                data.OperationName,
                data.DisplayName);

            // Use the start timestamp from the activity data.
            ((SpanTracer)span).StartTimestamp = data.StartTimeUtc;
        }
        else
        {
            // The parent span doesn't exist - start a new transaction.
            var context = new TransactionContext(
                data.SpanId.AsSentrySpanId(),
                data.ParentSpanId.AsSentrySpanId(),
                data.TraceId.AsSentryId(),
                data.DisplayName,
                data.OperationName,
                data.DisplayName,
                null,
                null,
                null
            );

            span = _hub.StartTransaction(context);

            // Use the start timestamp from the activity data.
            ((TransactionTracer)span).StartTimestamp = data.StartTimeUtc;
        }

        // span.Instrumenter = Instrumenter.Otel  // TODO - why?

        // Add the span to the map
        _map[data.SpanId] = span;
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        // Make a dictionary of the attributes (aka "tags") for faster lookup when used throughout the processor.
        var attributes = data.TagObjects.ToDictionary();

        if (IsSentryRequest(attributes))
        {
            // TODO: will this leave the span dangling?
            _map.TryRemove(data.SpanId, out _);
            return;
        }

        if (!_map.TryGetValue(data.SpanId, out var span))
        {
            _options?.DiagnosticLogger?.LogDebug("Span not found for SpanId: {0}", data.SpanId);
            return;
        }

        var (operation, description, source) = ParseOtelSpanDescription(data, attributes);
        span.Operation = operation;
        span.Description = description;

        if (span is TransactionTracer transaction)
        {
            transaction.Name = description;
            transaction.NameSource = source;

            // Use the end timestamp from the activity data.
            transaction.EndTimestamp = data.StartTimeUtc + data.Duration;

            // Transactions set otel attributes (and resource attributes) as context.
            transaction.Contexts["otel"] = GetOtelContext(attributes);
        }
        else
        {
            // Use the end timestamp from the activity data.
            ((SpanTracer)span).EndTimestamp = data.StartTimeUtc + data.Duration;

            // Spans set otel attributes in extras (passed to Sentry as "data" on the span).
            // Resource attributes do not need to be set, as they would be identical as those set on the transaction.
            span.SetExtras(attributes);
            span.SetExtra("otel.kind", data.Kind);
        }

        GenerateSentryErrorsFromOtelSpan(data);

        var status = GetSpanStatus(data.Status, attributes);
        span.Finish(status);

        _map.TryRemove(data.SpanId, out _);
    }

    private bool IsSentryRequest(IDictionary<string, object?> attributes)
    {
        if (_sentryBaseUrl is null)
        {
            return false;
        }

        if (attributes.TryGetTypedValue("http.url", out string url))
        {
            var requestBaseUrl = new Uri(url).GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
            if (string.Equals(requestBaseUrl, _sentryBaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static SpanStatus GetSpanStatus(ActivityStatusCode status, IDictionary<string, object?> attributes) =>
        status switch
        {
            ActivityStatusCode.Unset => SpanStatus.Ok,
            ActivityStatusCode.Ok => SpanStatus.Ok,
            ActivityStatusCode.Error => GetErrorSpanStatus(attributes),
            _ => SpanStatus.UnknownError
        };

    private static SpanStatus GetErrorSpanStatus(IDictionary<string, object?> attributes)
    {
        if (attributes.TryGetTypedValue("http.status_code", out int httpCode))
        {
            return SpanStatusConverter.FromHttpStatusCode(httpCode);
        }

        if (attributes.TryGetTypedValue("rpc.grpc.status_code", out int grpcCode))
        {
            return SpanStatusConverter.FromGrpcStatusCode(grpcCode);
        }

        return SpanStatus.UnknownError;
    }

    private static (string operation, string description, TransactionNameSource source) ParseOtelSpanDescription (
         Activity activity, IDictionary<string, object?> attributes)
    {
        // This function should loosely match the JavaScript implementation at:
        // https://github.com/getsentry/sentry-javascript/blob/develop/packages/opentelemetry-node/src/utils/parse-otel-span-description.ts
        // However, it should also follow the OpenTelemetry semantic conventions specification, as indicated.

        // HTTP span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/http/
        if (attributes.TryGetTypedValue("http.method", out string httpMethod))
        {
            if (activity.Kind == ActivityKind.Client)
            {
                // Per OpenTelemetry spec, client spans use only the method.
                return ("http.client", httpMethod, TransactionNameSource.Custom);
            }

            if (attributes.TryGetTypedValue("http.route", out string httpRoute))
            {
                // A route exists.  Use the method and route.
                return ("http.server", $"{httpMethod} {httpRoute}", TransactionNameSource.Route);
            }

            if (attributes.TryGetTypedValue("http.target", out string httpTarget))
            {
                // A target exists.  Use the method and target.  If the target is "/" we can treat it like a route.
                var source = httpTarget == "/" ? TransactionNameSource.Route : TransactionNameSource.Url;
                return ("http.server", $"{httpMethod} {httpTarget}", source);
            }

            // Some other type of HTTP server span.  Pass it through with the original name.
            return ("http.server", activity.DisplayName, TransactionNameSource.Custom);
        }

        // DB span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/database/
        if (attributes.ContainsKey("db.system"))
        {
            if (attributes.TryGetTypedValue("db.statement", out string dbStatement))
            {
                // We have a database statement.  Use it.
                return ("db", dbStatement, TransactionNameSource.Task);
            }

            // Some other type of DB span.  Pass it through with the original name.
            return ("db", activity.DisplayName, TransactionNameSource.Task);
        }

        // RPC span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/rpc/
        if (attributes.ContainsKey("rpc.service"))
        {
            return ("rpc", activity.DisplayName, TransactionNameSource.Route);
        }

        // Messaging span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/messaging/
        if (attributes.ContainsKey("messaging.system"))
        {
            return ("message", activity.DisplayName, TransactionNameSource.Route);
        }

        // FaaS (Functions/Lambda) span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/faas/
        if (attributes.TryGetTypedValue("faas.trigger", out string faasTrigger))
        {
            return (faasTrigger, activity.DisplayName, TransactionNameSource.Route);
        }

        // Default - pass through unmodified.
        return (activity.OperationName, activity.DisplayName, TransactionNameSource.Custom);
    }

    private Dictionary<string, object?> GetOtelContext(IDictionary<string, object?> attributes)
    {
        var otelContext = new Dictionary<string, object?>();
        if (attributes.Count > 0)
        {
            otelContext.Add("attributes", attributes);
        }

        var resourceAttributes = _resourceAttributes.Value;
        if (resourceAttributes.Count > 0)
        {
            otelContext.Add("resource", resourceAttributes);
        }

        return otelContext;
    }

    private void GenerateSentryErrorsFromOtelSpan(Activity activity, Dictionary<string, object?> spanAttributes)
    {
        // https://develop.sentry.dev/sdk/performance/opentelemetry/#step-7-define-generatesentryerrorsfromotelspan

        // TODO
    }
}
