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

            _options.DiagnosticLogger?.LogInfo("Received Sentry trace header: {0}", value);

            try
            {
                return SentryTraceHeader.Parse(value);
            }
            catch (Exception ex)
            {
                _options.DiagnosticLogger?.LogError("Invalid Sentry trace header: {0}", ex, value);
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

            await hub.ConfigureScopeAsync(async scope =>
            {
                // Attempt to start a transaction from the trace header if it exists
                var traceHeader = TryGetSentryTraceHeader(context);

                // Defer setting name until other middlewares have finished
                var transaction = traceHeader is not null
                    ? hub.StartTransaction(
                        "Unknown Route",
                        "http.server",
                        traceHeader
                    )
                    : hub.StartTransaction(
                        "Unknown Route",
                        "http.server"
                    );

                _options.DiagnosticLogger?.LogInfo(
                    "Started transaction: Span ID: {0}, Trace ID: {1}",
                    transaction.SpanId,
                    transaction.TraceId
                );

                // Put the transaction on the scope
                scope.Transaction = transaction;

                try
                {
                    await _next(context).ConfigureAwait(false);
                }
                finally
                {
                    // Try to resolve the route
                    if (context.TryGetTransactionName() is { } transactionName)
                    {
                        transaction.Name = transactionName;
                    }

                    transaction.Finish(
                        SpanStatusConverter.FromHttpStatusCode(context.Response.StatusCode)
                    );
                }
            }).ConfigureAwait(false);
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
