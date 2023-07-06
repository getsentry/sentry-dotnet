using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore.Extensions;
using Sentry.Extensibility;

namespace Sentry.AspNetCore;

/// <summary>
/// Sentry tracing middleware for ASP.NET Core
/// </summary>
internal class SentryTracingMiddleware
{
    private const string OperationName = "http.server";

    private readonly RequestDelegate _next;
    private readonly Func<IHub> _getHub;
    private readonly SentryAspNetCoreOptions _options;

    public SentryTracingMiddleware(
        RequestDelegate next,
        Func<IHub> getHub,
        IOptions<SentryAspNetCoreOptions> options)
    {
        _next = next;
        _getHub = getHub;
        _options = options.Value;
    }

    private ITransaction? TryStartTransaction(HttpContext context)
    {
        if (context.Request.Method == HttpMethod.Options.Method)
        {
            _options.LogInfo("No transaction started due to Options request.");
            return null;
        }

        try
        {
            var hub = _getHub();

            // Attempt to start a transaction from the trace header if it exists
            var traceHeader = context.TryGetSentryTraceHeader(_options);

            // It's important to try and set the transaction name to some value here so that it's available for use
            // in sampling.  At a later stage, we will try to get the transaction name again, to account for the
            // other middlewares that may have ran after ours.
            var transactionName = context.TryGetTransactionName() ?? string.Empty;

            var transactionContext = traceHeader is not null
                ? new TransactionContext(transactionName, OperationName, traceHeader, TransactionNameSource.Route)
                : new TransactionContext(transactionName, OperationName, TransactionNameSource.Route);

            var customSamplingContext = new Dictionary<string, object?>(4, StringComparer.Ordinal)
            {
                [SamplingExtensions.KeyForHttpMethod] = context.Request.Method,
                [SamplingExtensions.KeyForHttpRoute] = context.TryGetRouteTemplate(),
                [SamplingExtensions.KeyForHttpPath] = context.Request.Path.Value,
                [SamplingExtensions.KeyForHttpContext] = context,
            };

            // Set the Dynamic Sampling Context from the baggage header, if it exists.
            var baggageHeader = context.TryGetBaggageHeader(_options);
            var dynamicSamplingContext = baggageHeader?.CreateDynamicSamplingContext();

            if (traceHeader is { } && baggageHeader is null)
            {
                // We received a sentry-trace header without a baggage header, which indicates the request
                // originated from an older SDK that doesn't support dynamic sampling.
                // Set DynamicSamplingContext.Empty to "freeze" the DSC on the transaction.
                // See:
                // https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#freezing-dynamic-sampling-context
                // https://develop.sentry.dev/sdk/performance/dynamic-sampling-context/#unified-propagation-mechanism
                dynamicSamplingContext = DynamicSamplingContext.Empty;
            }

            var transaction = hub.StartTransaction(transactionContext, customSamplingContext, dynamicSamplingContext);

            _options.LogInfo(
                "Started transaction with span ID '{0}' and trace ID '{1}'.",
                transaction.SpanId,
                transaction.TraceId);

            return transaction;
        }
        catch (Exception ex)
        {
            _options.LogError("Failed to start transaction.", ex);
            return null;
        }
    }

    /// <summary>
    /// Handles the <see cref="HttpContext"/>.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        var hub = _getHub();

        if (!hub.IsEnabled)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        if (_options.Instrumenter == Instrumenter.OpenTelemetry)
        {
            _options.LogInfo(
                "When using OpenTelemetry instrumentation mode, the call to UseSentryTracing can be safely removed. " +
                "ASP.NET Core should be instrumented by following the instructions at " +
                "https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/src/OpenTelemetry.Instrumentation.AspNetCore/README.md");

            await _next(context).ConfigureAwait(false);
            return;
        }

        if (_options.TransactionNameProvider is { } route)
        {
            context.Features.Set(route);
        }

        var transaction = TryStartTransaction(context);
        var initialName = transaction?.Name;

        // Expose the transaction on the scope so that the user
        // can retrieve it and start child spans off of it.
        hub.ConfigureScope(scope =>
        {
            scope.Transaction = transaction;
        });

        Exception? exception = null;
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            exception = e;
        }
        finally
        {
            if (transaction is not null)
            {
                // The Transaction name was altered during the pipeline execution,
                // That could be done by user interference or by some Event Capture
                // That triggers ScopeExtensions.Populate.
                if (transaction.Name != initialName)
                {
                    _options.LogDebug(
                        "transaction name set from '{0}' to '{1}' during request pipeline execution.",
                        initialName,
                        transaction.Name);
                }
                // try to get the transaction name.
                else if (context.TryGetTransactionName() is { } transactionName &&
                         !string.IsNullOrEmpty(transactionName))
                {
                    _options.LogDebug(
                        "Changed transaction '{0}', name set to '{1}' after request pipeline executed.",
                        transaction.SpanId,
                        transactionName);

                    transaction.Name = transactionName;
                }

                var status = SpanStatusConverter.FromHttpStatusCode(context.Response.StatusCode);

                // If no Name was found for Transaction, then we don't have the route.
                if (transaction.Name == string.Empty)
                {
                    var method = context.Request.Method.ToUpperInvariant();

                    // If we've set a TransactionNameProvider, use that here
                    var customTransactionName = context.TryGetCustomTransactionName();
                    if (!string.IsNullOrEmpty(customTransactionName))
                    {
                        transaction.Name = $"{method} {customTransactionName}";
                        ((TransactionTracer)transaction).NameSource = TransactionNameSource.Custom;
                    }
                    else
                    {
                        // Finally, fallback to using the URL path.
                        // e.g. "GET /pets/1"
                        var path = context.Request.Path;
                        transaction.Name = $"{method} {path}";
                        ((TransactionTracer)transaction).NameSource = TransactionNameSource.Url;
                    }
                }

                if (exception is null)
                {
                    transaction.Finish(status);
                }
                // Status code not yet changed to 500 but an exception does exist
                // so lets avoid passing the misleading 200 down and close only with
                // the exception instance that will be inferred as errored.
                else if (status == SpanStatus.Ok)
                {
                    transaction.Finish(exception);
                }
                else
                {
                    transaction.Finish(exception, status);
                }
            }

            if (exception is not null)
            {
                ExceptionDispatchInfo.Capture(exception).Throw();
            }
        }
    }
}
