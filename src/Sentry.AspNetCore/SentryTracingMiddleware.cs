using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Sentry.AspNetCore.Extensions;
using Sentry.Protocol;

namespace Sentry.AspNetCore
{
    /// <summary>
    /// Sentry tracing middleware for ASP.NET Core
    /// </summary>
    internal class SentryTracingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Func<IHub> _hubAccessor;

        public SentryTracingMiddleware(RequestDelegate next, Func<IHub> hubAccessor)
        {
            _next = next;
            _hubAccessor = hubAccessor;
        }

        /// <summary>
        /// Handles the <see cref="HttpContext"/>.
        /// </summary>
        public async Task InvokeAsync(HttpContext context)
        {
            var hub = _hubAccessor();
            if (!hub.IsEnabled)
            {
                await _next(context).ConfigureAwait(false);
                return;
            }

            await hub.ConfigureScopeAsync(async scope =>
            {
                var transaction = hub.StartTransaction(
                    context.GetTransactionName(),
                    "http.server"
                );

                // Put the transaction on the scope
                scope.Transaction = transaction;

                try
                {
                    await _next(context).ConfigureAwait(false);
                }
                finally
                {
                    transaction.Finish(
                        GetSpanStatusFromCode(context.Response.StatusCode)
                    );
                }
            }).ConfigureAwait(false);
        }

        private static SpanStatus GetSpanStatusFromCode(int statusCode) => statusCode switch
        {
            < 400 => SpanStatus.Ok,
            400 => SpanStatus.InvalidArgument,
            401 => SpanStatus.Unauthenticated,
            403 => SpanStatus.PermissionDenied,
            404 => SpanStatus.NotFound,
            409 => SpanStatus.AlreadyExists,
            429 => SpanStatus.ResourceExhausted,
            499 => SpanStatus.Cancelled,
            < 500 => SpanStatus.InvalidArgument,
            500 => SpanStatus.InternalError,
            501 => SpanStatus.Unimplemented,
            503 => SpanStatus.Unavailable,
            504 => SpanStatus.DeadlineExceeded,
            < 600 => SpanStatus.InternalError,
            _ => SpanStatus.UnknownError
        };
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
