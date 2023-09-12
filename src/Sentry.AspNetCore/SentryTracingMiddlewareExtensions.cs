using Sentry;
using Sentry.AspNetCore;
using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for enabling <see cref="SentryTracingMiddleware"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryTracingMiddlewareExtensions
{
    private const string UseSentryTracingKey = "__UseSentryTracing";
    private const string InstrumenterKey = "__SentryInstrumenter";

    internal static bool IsSentryTracingRegistered(this IApplicationBuilder builder)
        => builder.Properties.ContainsKey(UseSentryTracingKey);
    internal static void StoreInstrumenter(this IApplicationBuilder builder, Instrumenter instrumenter)
        => builder.Properties[InstrumenterKey] = instrumenter;

    internal static bool ShouldRegisterSentryTracing(this IApplicationBuilder builder)
    {
        if (builder.Properties.ContainsKey(UseSentryTracingKey))
        {
            return false;
        }
        if (builder.Properties.TryGetTypedValue(InstrumenterKey, out Instrumenter instrumenter))
        {
            return instrumenter == Instrumenter.Sentry;
        }
        return true;
    }

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
