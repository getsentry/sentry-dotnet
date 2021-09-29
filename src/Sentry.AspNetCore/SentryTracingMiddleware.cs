using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore.Extensions;
using Sentry.Extensibility;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry tracing middleware for ASP.NET Core
    /// </summary>
    internal class SentryTracingMiddleware
    {
        private const string UnknownRouteTransactionName = "Unknown Route";
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

        private SentryTraceHeader? TryGetSentryTraceHeader(HttpContext context)
        {
            var value = context.Request.Headers.GetValueOrDefault(SentryTraceHeader.HttpHeaderName);
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            _options.DiagnosticLogger?.LogDebug("Received Sentry trace header '{0}'.", value);

            try
            {
                return SentryTraceHeader.Parse(value);
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Invalid Sentry trace header '{0}'.", ex, value);
                return null;
            }
        }

        private ITransaction? TryStartTransaction(HttpContext context)
        {
            try
            {
                var hub = _getHub();

                // Attempt to start a transaction from the trace header if it exists
                var traceHeader = TryGetSentryTraceHeader(context);

                // It's important to try and set the transaction name
                // to some value here so that it's available for use
                // in sampling.
                // At a later stage, we will try to get the transaction name
                // again, to account for the other middlewares that may have
                // ran after ours.
                var transactionName =
                    context.TryGetTransactionName() ??
                    UnknownRouteTransactionName;

                var transactionContext = traceHeader is not null
                    ? new TransactionContext(transactionName, OperationName, traceHeader)
                    : new TransactionContext(transactionName, OperationName);

                var customSamplingContext = new Dictionary<string, object?>(3, StringComparer.Ordinal)
                {
                    [SamplingExtensions.KeyForHttpMethod] = context.Request.Method,
                    [SamplingExtensions.KeyForHttpRoute] = context.TryGetRouteTemplate(),
                    [SamplingExtensions.KeyForHttpPath] = context.Request.Path.Value
                };

                var transaction = hub.StartTransaction(transactionContext, customSamplingContext);

                _options.DiagnosticLogger?.LogInfo(
                    "Started transaction with span ID '{0}' and trace ID '{1}'.",
                    transaction.SpanId,
                    transaction.TraceId);

                return transaction;
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Failed to start transaction.", ex);
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

            var transaction = TryStartTransaction(context);

            // Expose the transaction on the scope so that the user
            // can retrieve it and start child spans off of it.
            hub.ConfigureScope(scope =>
            {
                scope.Transaction = transaction;
                scope.OnEvaluating += (_, _) => scope.Populate(context, _options);
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
                    // The routing middleware may have ran after ours, so
                    // try to get the transaction name again.
                    if (context.TryGetTransactionName() is { } transactionName)
                    {
                        if (!string.Equals(transaction.Name, transactionName, StringComparison.Ordinal))
                        {
                            _options.DiagnosticLogger?.LogDebug(
                                "Changed transaction name from '{0}' to '{1}' after request pipeline executed.",
                                transaction.Name,
                                transactionName);
                        }

                        transaction.Name = transactionName;
                    }

                    var status = SpanStatusConverter.FromHttpStatusCode(context.Response.StatusCode);
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
}

namespace Microsoft.AspNetCore.Builder
{
    using Sentry.AspNetCore;

    /// <summary>
    /// Extensions for enabling <see cref="SentryTracingMiddleware"/>.
    /// </summary>
    public static class SentryTracingMiddlewareExtensions
    {
        /// <summary>
        /// Adds Sentry's tracing middleware to the pipeline.
        /// Make sure to place this middleware after <code>UseRouting(...)</code>.
        /// </summary>
        public static IApplicationBuilder UseSentryTracing(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<SentryTracingMiddleware>();
            return builder;
        }
    }
}
