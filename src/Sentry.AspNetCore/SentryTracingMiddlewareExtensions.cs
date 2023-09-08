using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for enabling <see cref="SentryTracingMiddleware"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryTracingMiddlewareExtensions
{
    private const string UseSentryTracingKey = "__UseSentryTracing";

    internal static bool IsSentryTracingRegistered(this IApplicationBuilder builder)
        => builder.Properties.ContainsKey(UseSentryTracingKey);

    /// <summary>
    /// Adds Sentry's tracing middleware to the pipeline.
    /// Make sure to place this middleware after <c>UseRouting(...)</c>.
    /// </summary>
    public static IApplicationBuilder UseSentryTracing(this IApplicationBuilder builder)
    {
        // Don't register twice
        if (builder.IsSentryTracingRegistered())
        {
            return builder;
        }

        builder.Properties[UseSentryTracingKey] = true;
        builder.UseMiddleware<SentryTracingMiddleware>();
        return builder;
    }
}
