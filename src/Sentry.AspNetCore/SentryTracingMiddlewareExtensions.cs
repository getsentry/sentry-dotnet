using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for enabling <see cref="SentryTracingMiddleware"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryTracingMiddlewareExtensions
{
    /// <summary>
    /// Adds Sentry's tracing middleware to the pipeline.
    /// Make sure to place this middleware after <c>UseRouting(...)</c>.
    /// </summary>
    public static IApplicationBuilder UseSentryTracing(this IApplicationBuilder builder)
    {
        builder.UseMiddleware<SentryTracingMiddleware>();
        return builder;
    }
}
