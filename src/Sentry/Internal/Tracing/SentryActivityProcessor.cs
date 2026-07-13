#if HAS_ACTIVITY_LISTENER
using Sentry.Extensibility;
using Sentry.Internal.Extensions;
using Sentry.Internal.OpenTelemetry;

namespace Sentry.Internal.Tracing;

/// <summary>
/// Converts <see cref="Activity"/> lifecycle events into Sentry transactions and spans.
/// </summary>
/// <remarks>
/// This is a port of Sentry.OpenTelemetry.SentrySpanProcessor with the OpenTelemetry SDK dependency removed:
/// instead of deriving from OpenTelemetry's BaseProcessor&lt;Activity&gt;, it is driven by an
/// <see cref="ActivityListener"/> (see <see cref="SentryActivityListener"/>). The only functionality lost in
/// the port is OTel Resource attribute detection (ParentProvider.GetResource() has no System.Diagnostics
/// equivalent) — resource attributes can instead be supplied via the constructor.
/// </remarks>
internal class SentryActivityProcessor
{
    private readonly IHub _hub;
    internal readonly IEnumerable<ISentryActivityEnricher> _enrichers;
    private readonly IReplaySession _replaySession;
    internal const string OpenTelemetryOrigin = "auto.otel";

    // ReSharper disable once MemberCanBePrivate.Global - Used by tests
    internal readonly ConcurrentDictionary<ActivitySpanId, ISpan> _map = new();
    private readonly SentryOptions? _options;
    private readonly Lazy<IDictionary<string, object>> _resourceAttributes;

    private static readonly long PruningInterval = TimeSpan.FromSeconds(5).Ticks;
    internal long _lastPruned = 0;
    private readonly Lazy<Hub?> _realHub;

    internal SentryActivityProcessor(
        IHub hub,
        IEnumerable<ISentryActivityEnricher>? enrichers = null,
        IReplaySession? replaySession = null,
        Func<IDictionary<string, object>>? resourceAttributeResolver = null)
    {
        _hub = hub;
        _realHub = new Lazy<Hub?>(() =>
            _hub switch
            {
                Hub thisHub => thisHub,
                HubAdapter when SentrySdk.CurrentHub is Hub sdkHub => sdkHub,
                _ => null
            });

        if (_hub is DisabledHub)
        {
            // This would only happen if someone tried to create a SentryActivityProcessor manually
            throw new InvalidOperationException(
                "Attempted to create a SentryActivityProcessor for a Disabled hub. " +
                "The Sentry SDK should be initialized before the activity processor is created.");
        }

        _enrichers = enrichers ?? [];
        _replaySession = replaySession ?? ReplaySession.Instance;
        _options = hub.GetSentryOptions();

        if (_options is null)
        {
            throw new InvalidOperationException(
                "The Sentry SDK has not been initialised. To capture tracing instrumentation from Activities " +
                "you need to initialize the Sentry SDK.");
        }

        // Spike note: retained for behavioural parity with SentrySpanProcessor. In a real Activity-based core
        // this guard inverts — Instrumenter.OpenTelemetry marks spans created from Activities so that parent
        // inference (see OnStart) can distinguish them from Sentry-native spans.
        if (_options.Instrumenter != Instrumenter.OpenTelemetry)
        {
            throw new InvalidOperationException(
                "Activity-based tracing has not been configured on the Sentry SDK. You need " +
                "to initialize the Sentry SDK with options.Instrumenter = Instrumenter.OpenTelemetry");
        }

        // OTel Resource attributes have no System.Diagnostics equivalent; callers may supply them instead.
        // Resolved lazily (once) as they are consistent between spans.
        _resourceAttributes = new Lazy<IDictionary<string, object>>(
            resourceAttributeResolver ?? (static () => new Dictionary<string, object>(0)));
    }

    public void OnStart(Activity data)
    {
        if (!_hub.IsEnabled)
        {
            // This would be unusual... it might happen if the SDK is closed while the processor is still running and
            // we receive new telemetry. In this case, we can't log anything because our logger is disabled, so we just
            // swallow it
            return;
        }

        if (data.ParentSpanId != default && _map.TryGetValue(data.ParentSpanId, out var mappedParent))
        {
            // Explicit ParentSpanId of another activity that we have already mapped
            CreateChildSpan(data, mappedParent, data.ParentSpanId);
        }
        // Note if the current span on the hub is OTel instrumented and is not the parent of `data` then this may be
        // intentional (see https://opentelemetry.io/docs/languages/net/instrumentation/#creating-new-root-activities)
        // so we explicitly exclude OTel instrumented spans from the following check.
        else if (_hub.GetSpan() is IBaseTracer { IsOtelInstrumenter: false } inferredParent)
        {
            // When mixing Sentry and Activity instrumentation and we infer that the currently active span is the parent.
            var inferredParentSpan = (ISpan)inferredParent;
            CreateChildSpan(data, inferredParentSpan, inferredParentSpan.SpanId);
        }
        else
        {
            CreateRootSpan(data);
        }

        // Housekeeping
        PruneFilteredSpans();
    }

    private void CreateChildSpan(Activity data, ISpan parentSpan, ActivitySpanId? parentSpanId = null)
        => CreateChildSpan(data, parentSpan, parentSpanId?.AsSentrySpanId());

    private void CreateChildSpan(Activity data, ISpan parentSpan, SpanId? parentSpanId = null)
    {
        // We can find the parent span - start a child span.
        var context = new SpanContext(
            data.OperationName,
            data.SpanId.AsSentrySpanId(),
            parentSpanId,
            description: data.DisplayName
        )
        {
            Instrumenter = Instrumenter.OpenTelemetry
        };

        var span = parentSpan.StartChild(context);
        // Used to filter out spans that are not recorded when finishing a transaction
        span.SetFused(data);
        if (span is SpanTracer spanTracer)
        {
            spanTracer.Origin = OpenTelemetryOrigin;
            spanTracer.StartTimestamp = data.StartTimeUtc;
            spanTracer.IsFiltered = () => spanTracer.GetFused<Activity>() is { IsAllDataRequested: false, Recorded: false };
        }
        _map[data.SpanId] = span;
    }

    private void CreateRootSpan(Activity data)
    {
        // If a parent exists at all, then copy its sampling decision.
        bool? isSampled = data.HasRemoteParent ? data.Recorded : null;

        // No parent span found - start a new transaction
        var transactionContext = new TransactionContext(
            data.DisplayName,
            data.OperationName,
            data.SpanId.AsSentrySpanId(),
            data.ParentSpanId.AsSentrySpanId(),
            data.TraceId.AsSentryId(),
            data.DisplayName, null, isSampled, isSampled)
        {
            Instrumenter = Instrumenter.OpenTelemetry
        };

        var baggageHeader = data.Baggage.AsBaggageHeader();
        var dynamicSamplingContext = baggageHeader.CreateDynamicSamplingContext(_replaySession);
        var transaction = _hub.StartTransaction(
            transactionContext, new Dictionary<string, object?>(), dynamicSamplingContext
        );
        if (transaction is TransactionTracer tracer)
        {
            tracer.Contexts.Trace.Origin = OpenTelemetryOrigin;
            tracer.StartTimestamp = data.StartTimeUtc;
        }
        _hub.ConfigureScope(static (scope, transaction) => scope.Transaction = transaction, transaction);
        transaction.SetFused(data);
        _map[data.SpanId] = transaction;
    }

    public void OnEnd(Activity data)
    {
        if (!_hub.IsEnabled)
        {
            // This would be unusual... it might happen if the SDK is closed while the processor is still running and
            // we receive new telemetry. In this case, we can't log anything because our logger is disabled, so we just
            // swallow it
            return;
        }

        // Skip any activities that are not recorded.
        if (data is { Recorded: false })
        {
            _options?.DiagnosticLogger?.LogDebug("Ignoring unrecorded Activity {0}.", data.SpanId);
            _map.TryRemove(data.SpanId, out _);
            return;
        }

        // Make a dictionary of the attributes (aka "tags") for faster lookup when used throughout the processor.
        var attributes = data.TagObjects.ToDict();

        var url = attributes.UrlFullAttribute();
        if (!string.IsNullOrEmpty(url) && (_options?.IsSentryRequest(url) ?? false))
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

        // Handle HTTP response status code specially
        var statusCode = attributes.HttpResponseStatusCodeAttribute();
        if (span is TransactionTracer transaction)
        {
            transaction.Name = description;
            transaction.NameSource = source;
            if (statusCode is { } responseStatusCode)
            {
                transaction.Contexts.Response.StatusCode = responseStatusCode;
                transaction.SetData(OtelSemanticConventions.AttributeHttpResponseStatusCode, responseStatusCode);
            }

            // Use the end timestamp from the activity data.
            transaction.EndTimestamp = data.StartTimeUtc + data.Duration;

            // Transactions set otel attributes (and resource attributes) as context.
            transaction.Contexts["otel"] = GetOtelContext(attributes);
        }
        else if (span is SpanTracer spanTracer)
        {
            // Use the end timestamp from the activity data.
            spanTracer.EndTimestamp = data.StartTimeUtc + data.Duration;

            // Spans set otel attributes in extras (passed to Sentry as "data" on the span).
            // Resource attributes do not need to be set, as they would be identical as those set on the transaction.
            spanTracer.SetExtras(attributes);
            spanTracer.SetExtra("otel.kind", data.Kind);
            if (statusCode is { } responseStatusCode)
            {
                // Set this as a tag so that it's searchable in Sentry
                span.SetTag(OtelSemanticConventions.AttributeHttpResponseStatusCode, responseStatusCode.ToString());
            }
        }

        // In ASP.NET Core the middleware finishes up (and the scope gets popped) before the activity is ended.  So we
        // need to restore the scope here (it's saved by our middleware when the request starts)
        var activityScope = GetSavedScope(data);
        if (activityScope is { } savedScope)
        {
            var hub = _realHub.Value;
            hub?.RestoreScope(savedScope);
        }
        GenerateSentryErrorsFromOtelSpan(data, attributes);

        var status = GetSpanStatus(data.Status, attributes);
        foreach (var enricher in _enrichers)
        {
            enricher.Enrich(span, data, _hub, _options);
        }
        span.Finish(status);

        _map.TryRemove(data.SpanId, out _);

        // Housekeeping
        PruneFilteredSpans();
    }

    /// <summary>
    /// Clean up items that may have been filtered out.
    /// See https://github.com/getsentry/sentry-dotnet/pull/3198
    /// </summary>
    internal void PruneFilteredSpans(bool force = false)
    {
        if (!force && !NeedsPruning())
        {
            return;
        }

        foreach (var mappedItem in _map)
        {
            var (spanId, span) = mappedItem;
            var activity = span.GetFused<Activity>();
            // Also prune when the activity has been GC'd (weak ref returns null): the activity is gone, so it
            // can never call OnEnd, and the span will never be removed otherwise — causing a memory leak.
            if (activity is null or { Recorded: false, IsAllDataRequested: false })
            {
                _map.TryRemove(spanId, out _);
            }
        }
    }

    private bool NeedsPruning()
    {
        var lastPruned = Interlocked.Read(ref _lastPruned);
        if (lastPruned > DateTime.UtcNow.Ticks - PruningInterval)
        {
            return false;
        }

        var thisPruned = DateTime.UtcNow.Ticks;
        Interlocked.CompareExchange(ref _lastPruned, thisPruned, lastPruned);
        // May be false if another thread gets there first
        return Interlocked.Read(ref _lastPruned) == thisPruned;
    }

    private static Scope? GetSavedScope(Activity? activity)
    {
        while (activity is not null)
        {
            if (activity.GetFused<Scope>() is { } savedScope)
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
        return status switch
        {
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

    internal static (string operation, string description, TransactionNameSource source) ParseOtelSpanDescription(
        Activity activity,
        IDictionary<string, object?> attributes)
    {
        // This function should loosely match the JavaScript implementation at:
        // https://github.com/getsentry/sentry-javascript/blob/3487fa3af7aa72ac7fdb0439047cb7367c591e77/packages/opentelemetry-node/src/utils/parseOtelSpanDescription.ts
        // However, it should also follow the OpenTelemetry semantic conventions specification, as indicated.

        // HTTP span
        // https://opentelemetry.io/docs/specs/otel/trace/semantic_conventions/http/
        if (attributes.HttpMethodAttribute() is { } httpMethod)
        {
            if (activity.Kind == ActivityKind.Client)
            {
                // Per OpenTelemetry spec, client spans use only the method.
                var description = (attributes.UrlFullAttribute() is { } fullUrl)
                    ? $"{httpMethod} {fullUrl}"
                    : httpMethod;
                return ("http.client", description, TransactionNameSource.Custom);
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
            var eventAttributes = @event.Tags.ToDict();
            // This would be where we would ideally implement full exception capture. That's not possible at the
            // moment since the full exception isn't yet available via the OpenTelemetry API.
            // See https://github.com/open-telemetry/opentelemetry-dotnet/issues/2439#issuecomment-1577314568
            if (!eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionType, out string exceptionType))
            {
                continue;
            }
            eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionMessage, out string message);
            eventAttributes.TryGetTypedValue(OtelSemanticConventions.AttributeExceptionStacktrace, out string stackTrace);

            Exception exception;
            try
            {
                if (CreatePoorMansException(exceptionType, message) is not { } poorMansException)
                {
                    _options?.DiagnosticLogger?.LogWarning($"Unable to create poor man's exception with trimming enabled : {exceptionType}");
                    continue;
                }
                exception = poorMansException;
            }
            catch
            {
                _options?.DiagnosticLogger?.LogError($"Failed to create poor man's exception for type : {exceptionType}");
                continue;
            }

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

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = AotHelper.AvoidAtRuntime)]
    private static Exception? CreatePoorMansException(string exceptionType, string message)
    {
        if (AotHelper.IsTrimmed)
        {
            return null;
        }

        var type = Type.GetType(exceptionType)!;
        var exception = (Exception)Activator.CreateInstance(type, message)!;
        exception.SetSentryMechanism("SentryActivityProcessor.ErrorSpan");
        return exception;
    }
}
#endif
