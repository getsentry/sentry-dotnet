using OpenTelemetry;
using Sentry.Extensibility;
using Sentry.Internal;
using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.OpenTelemetry;

/// <summary>
/// Sentry span processor for Open Telemetry.
/// </summary>
public class SentrySpanProcessor : BaseProcessor<Activity>
{
    private readonly IHub _hub;
    private readonly IEnumerable<IOpenTelemetryEnricher> _enrichers;

    // ReSharper disable once MemberCanBePrivate.Global - Used by tests
    internal readonly ConcurrentDictionary<ActivitySpanId, ISpan> _map = new();
    private readonly SentryOptions? _options;
    private readonly Lazy<IDictionary<string, object>> _resourceAttributes;

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor() : this(SentrySdk.CurrentHub)
    {
    }

    /// <summary>
    /// Constructs a <see cref="SentrySpanProcessor"/>.
    /// </summary>
    public SentrySpanProcessor(IHub hub) : this(hub, null)
    {
    }

    internal SentrySpanProcessor(IHub hub, IEnumerable<IOpenTelemetryEnricher>? enrichers)
    {
        _hub = hub;
        _enrichers = enrichers ?? Enumerable.Empty<IOpenTelemetryEnricher>();
        _options = hub.GetSentryOptions();

        if (_options is not { })
        {
            throw new InvalidOperationException(
                "The Sentry SDK has not been initialised. To use Sentry with OpenTelemetry tracing you need to " +
                "initialize the Sentry SDK.");
        }

        if (_options.Instrumenter != Instrumenter.OpenTelemetry)
        {
            throw new InvalidOperationException(
                "OpenTelemetry has not been configured on the Sentry SDK. To use OpenTelemetry tracing you need " +
                "to initialize the Sentry SDK with options.UseOpenTelemetry()");
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
                data.OperationName,
                data.SpanId.AsSentrySpanId(),
                data.ParentSpanId.AsSentrySpanId(),
                data.TraceId.AsSentryId(),
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
            var transactionContext = new TransactionContext(data.DisplayName,
                data.OperationName,
                data.SpanId.AsSentrySpanId(),
                data.ParentSpanId.AsSentrySpanId(),
                data.TraceId.AsSentryId(),
                data.DisplayName, null, isSampled, isSampled)
            {
                Instrumenter = Instrumenter.OpenTelemetry
            };

            var baggageHeader = data.Baggage.AsBaggageHeader();
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

        if (attributes.TryGetTypedValue("http.url", out string? url) && (_options?.IsSentryRequest(url) ?? false))
        {
            _options?.DiagnosticLogger?.LogDebug($"Ignoring Activity {data.SpanId} for Sentry request.");

            if (_map.TryRemove(data.SpanId, out var removed))
            {
                if (removed is SpanTracer spanTracerToRemove)
                {
                    spanTracerToRemove.IsSentryRequest = true;
                }

                if (removed is TransactionTracer transactionTracer)
                {
                    transactionTracer.IsSentryRequest = true;
                }
            }

            return;
        }

        if (!_map.TryGetValue(data.SpanId, out var span))
        {
            _options?.DiagnosticLogger?.LogError($"Span not found for SpanId: {data.SpanId}. Did OnStart run? We might have a bug in the SDK.");
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

        // Events are received/processed in a different AsyncLocal context. Restoring the scope that started it.
        var activityScope = GetSavedScope(data);
        if (activityScope is { } savedScope && _hub is Hub hub)
        {
            hub.RestoreScope(savedScope);
        }
        GenerateSentryErrorsFromOtelSpan(data, attributes);

        var status = GetSpanStatus(data.Status, attributes);
        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(span, data, _hub, _options);
        }
        span.Finish(status);

        _map.TryRemove(data.SpanId, out _);
    }

    private static Scope? GetSavedScope(Activity? activity)
    {
        while (activity is not null)
        {
            if (activity.GetFused<Scope>() is {} savedScope)
            {
                return savedScope;
            }
            activity = activity.Parent;
        }
        return null;
    }

    internal static SpanStatus GetSpanStatus(ActivityStatusCode status, IDictionary<string, object?> attributes)
    {
        // See https://github.com/open-telemetry/opentelemetry-dotnet/discussions/4703
        if (attributes.TryGetValue(OtelSpanAttributeConstants.StatusCodeKey, out var statusCode)
            && statusCode is OtelStatusTags.ErrorStatusCodeTagValue
           )
        {
            return GetErrorSpanStatus(attributes);
        }
        return status switch {
            ActivityStatusCode.Unset => SpanStatus.Ok,
            ActivityStatusCode.Ok => SpanStatus.Ok,
            ActivityStatusCode.Error => GetErrorSpanStatus(attributes),
            _ => SpanStatus.UnknownError
        };
    }

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

    private static (string operation, string description, TransactionNameSource source) ParseOtelSpanDescription(
         Activity activity,
         IDictionary<string, object?> attributes)
    {
        // This function should loosely match the JavaScript implementation at:
        // https://github.com/getsentry/sentry-javascript/blob/3487fa3af7aa72ac7fdb0439047cb7367c591e77/packages/opentelemetry-node/src/utils/parseOtelSpanDescription.ts
        // However, it should also follow the OpenTelemetry semantic conventions specification, as indicated.

        // HTTP span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/http/
        if (attributes.TryGetTypedValue(OtelSemanticConventions.AttributeHttpMethod, out string httpMethod))
        {
            if (activity.Kind == ActivityKind.Client)
            {
                // Per OpenTelemetry spec, client spans use only the method.
                return ("http.client", httpMethod, TransactionNameSource.Custom);
            }

            if (attributes.TryGetTypedValue(OtelSemanticConventions.AttributeHttpRoute, out string httpRoute))
            {
                // A route exists.  Use the method and route.
                return ("http.server", $"{httpMethod} {httpRoute}", TransactionNameSource.Route);
            }

            if (attributes.TryGetTypedValue(OtelSemanticConventions.AttributeHttpTarget, out string httpTarget))
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
        if (attributes.ContainsKey(OtelSemanticConventions.AttributeDbSystem))
        {
            if (attributes.TryGetTypedValue(OtelSemanticConventions.AttributeDbStatement, out string dbStatement))
            {
                // We have a database statement.  Use it.
                return ("db", dbStatement, TransactionNameSource.Task);
            }

            // Some other type of DB span.  Pass it through with the original name.
            return ("db", activity.DisplayName, TransactionNameSource.Task);
        }

        // RPC span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/rpc/
        if (attributes.ContainsKey(OtelSemanticConventions.AttributeRpcService))
        {
            return ("rpc", activity.DisplayName, TransactionNameSource.Route);
        }

        // Messaging span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/messaging/
        if (attributes.ContainsKey(OtelSemanticConventions.AttributeMessagingSystem))
        {
            return ("message", activity.DisplayName, TransactionNameSource.Route);
        }

        // FaaS (Functions/Lambda) span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/faas/
        if (attributes.TryGetTypedValue(OtelSemanticConventions.AttributeFaasTrigger, out string faasTrigger))
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
        // https://develop.sentry.dev/sdk/performance/opentelemetry/#step-7-define-generatesentryerrorsfromotelspan
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/exceptions/
        foreach (var @event in activity.Events.Where(e => e.Name == OtelSemanticConventions.AttributeExceptionEventName))
        {
            var eventAttributes = @event.Tags.ToDictionary();
            // This would be where we would ideally implement full exception capture. That's not possible at the
            // moment since the full exception isn't yet available via the OpenTelemetry API.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/issues/2439#issuecomment-1577314568
            // if (!eventAttributes.TryGetTypedValue("exception", out Exception exception))
            // {
            //      continue;
            // }

            // At the moment, OTEL only gives us `exception.type`, `exception.message`, and `exception.stacktrace`...
            // So the best we can do is a poor man's exception (no accurate symbolication or anything)
            if (!eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionType, out string exceptionType))
            {
                continue;
            }
            eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionMessage, out string message);
            eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionStacktrace, out string stackTrace);

            Exception exception;
            try
            {
                var type = Type.GetType(exceptionType)!;
                exception = (Exception)Activator.CreateInstance(type, message)!;
                exception.SetSentryMechanism("SentrySpanProcessor.ErrorSpan");
            }
            catch
            {
                _options?.DiagnosticLogger?.LogError($"Failed to create poor man's exception for type : {exceptionType}");
                continue;
            }

            // TODO: Validate that our `DuplicateEventDetectionEventProcessor` prevents this from doubling exceptions
            // that are also caught by other means, such as our AspNetCore middleware, etc.
            // (When options.RecordException = true is set on AddAspNetCoreInstrumentation...)
            // Also, in such cases - how will we get the otel scope and trace context on the other one?

            var sentryEvent = new SentryEvent(exception, @event.Timestamp);
            var otelContext = GetOtelContext(spanAttributes);
            otelContext.Add("stack_trace", stackTrace);
            sentryEvent.Contexts["otel"] = otelContext;
            _hub.CaptureEvent(sentryEvent, scope =>
            {
                var trace = scope.Contexts.Trace;
                trace.SpanId = activity.SpanId.AsSentrySpanId();
                trace.ParentSpanId = activity.ParentSpanId.AsSentrySpanId();
                trace.TraceId = activity.TraceId.AsSentryId();
            });
        }
    }
}
