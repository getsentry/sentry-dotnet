using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
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

                var transaction = traceHeader is not null
                    ? hub.StartTransaction(
                        transactionName,
                        OperationName,
                        traceHeader
                    )
                    : hub.StartTransaction(
                        transactionName,
                        OperationName
                    );

                _options.DiagnosticLogger?.LogInfo(
                    "Started transaction with span ID '{0}' and trace ID '{1}'.",
                    transaction.SpanId,
                    transaction.TraceId
                );

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
            hub.ConfigureScope(scope => scope.Transaction = transaction);

            try
            {
                await _next(context).ConfigureAwait(false);
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
                                transactionName
                            );
                        }

                        transaction.Name = transactionName;
                    }

                    transaction.Finish(
                        SpanStatusConverter.FromHttpStatusCode(context.Response.StatusCode)
                    );
                }
            }
        }
    }

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
