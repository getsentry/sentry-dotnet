using OpenTelemetry;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

namespace Sentry.OpenTelemetry;

// https://develop.sentry.dev/sdk/performance/opentelemetry

/// <summary>
/// Sentry span processor for Open Telemetry.
/// </summary>
public class SentrySpanProcessor : BaseProcessor<Activity>
{
    private readonly IHub _hub;

    // ReSharper disable once MemberCanBePrivate.Global - Used by tests
    internal readonly ConcurrentDictionary<ActivitySpanId, ISpan> _map = new();
    private readonly SentryOptions? _options;
    private readonly Lazy<IDictionary<string, object>> _resourceAttributes;

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor(IHub hub)
    {
        _hub = hub;
        _options = hub.GetSentryOptions();

        if (_options?.Instrumenter != Instrumenter.OpenTelemetry)
        {
            throw new InvalidOperationException(
                $"To use the {nameof(SentrySpanProcessor)}, you must also set the " +
                $"{nameof(SentryOptions.Instrumenter)} option to {nameof(Instrumenter.OpenTelemetry)} " +
                "when initializing Sentry.");
        }

        // Resource attributes are consistent between spans, but not available during construction.
        // Thus, get a single instance lazily.
        _resourceAttributes = new Lazy<IDictionary<string, object>>(() =>
            ParentProvider?.GetResource().Attributes.ToDictionary() ?? new Dictionary<string, object>(0));
    }

    /// <inheritdoc />
    public override void OnStart(Activity data)
    {
        if (data.ParentSpanId != default && _map.TryGetValue(data.ParentSpanId, out var parentSpan))
        {
            // We can find the parent span - start a child span.
            var context = new SpanContext(
                data.SpanId.AsSentrySpanId(),
                data.ParentSpanId.AsSentrySpanId(),
                data.TraceId.AsSentryId(),
                data.OperationName,
                data.DisplayName,
                null,
                null)
            {
                Instrumenter = Instrumenter.OpenTelemetry
            };

            var span = (SpanTracer)parentSpan.StartChild(context);
            span.StartTimestamp = data.StartTimeUtc;
            _map[data.SpanId] = span;
        }
        else
        {
            // If a parent exists at all, then copy its sampling decision.
            bool? isSampled = data.HasRemoteParent ? data.Recorded : null;

            // No parent span found - start a new transaction
            var transactionContext = new TransactionContext(
                data.SpanId.AsSentrySpanId(),
                data.ParentSpanId.AsSentrySpanId(),
                data.TraceId.AsSentryId(),
                data.DisplayName,
                data.OperationName,
                data.DisplayName,
                null,
                isSampled,
                isSampled)
            {
                Instrumenter = Instrumenter.OpenTelemetry
            };

            var baggageHeader = BaggageHeader.Create(data.Baggage.WithValues());
            var dynamicSamplingContext = baggageHeader.CreateDynamicSamplingContext();
            var transaction = (TransactionTracer)_hub.StartTransaction(
                transactionContext, new Dictionary<string, object?>(), dynamicSamplingContext
                );
            transaction.StartTimestamp = data.StartTimeUtc;
            _map[data.SpanId] = transaction;
        }
    }

    /// <inheritdoc />
    public override void OnEnd(Activity data)
    {
        // Make a dictionary of the attributes (aka "tags") for faster lookup when used throughout the processor.
        var attributes = data.TagObjects.ToDictionary();

        if (attributes.TryGetTypedValue("http.url", out string url) && _hub.IsSentryRequest(url))
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

        GenerateSentryErrorsFromOtelSpan(data, attributes);

        var status = GetSpanStatus(data.Status, attributes);
        span.Finish(status);

        _map.TryRemove(data.SpanId, out _);
    }

    internal static SpanStatus GetSpanStatus(ActivityStatusCode status, IDictionary<string, object?> attributes) =>
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

    private void GenerateSentryErrorsFromOtelSpan(Activity activity, IDictionary<string, object?> spanAttributes)
    {
    //     // https://develop.sentry.dev/sdk/performance/opentelemetry/#step-7-define-generatesentryerrorsfromotelspan
    //     // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/exceptions/
    //
    //     foreach (var @event in activity.Events.Where(e => e.Name == "exception"))
    //     {
    //         // Note, this doesn't do anything yet because `exception` is not a valid attribute.
    //         // We cannot just use `exception.type`, `exception.message`, and `exception.stacktrace`.
    //         // See https://github.com/open-telemetry/opentelemetry-dotnet/issues/2439#issuecomment-1577314568
    //
    //         var eventAttributes = @event.Tags.ToDictionary();
    //         if (!eventAttributes.TryGetTypedValue("exception", out Exception exception))
    //         {
    //             continue;
    //         }
    //
    //         // TODO: Validate that our `DuplicateEventDetectionEventProcessor` prevents this from doubling exceptions
    //         // that are also caught by other means, such as our AspNetCore middleware, etc.
    //         // (When options.RecordException = true is set on AddAspNetCoreInstrumentation...)
    //         // Also, in such cases - how will we get the otel scope and trace context on the other one?
    //
    //         var sentryEvent = new SentryEvent(exception, @event.Timestamp);
    //         _hub.CaptureEvent(sentryEvent, scope =>
    //         {
    //             scope.Contexts["otel"] = GetOtelContext(spanAttributes);
    //
    //             var trace = scope.Contexts.Trace;
    //             trace.TraceId = activity.TraceId.AsSentryId();
    //             trace.SpanId = activity.SpanId.AsSentrySpanId();
    //             trace.ParentSpanId = activity.ParentSpanId.AsSentrySpanId();
    //         });
    //     }
    }
}
