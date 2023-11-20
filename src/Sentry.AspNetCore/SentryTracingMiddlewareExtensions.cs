using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore;
using Sentry.Extensibility;
using Sentry.Internal.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extensions for enabling <see cref="SentryTracingMiddleware"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryTracingMiddlewareExtensions
{
    internal const string AlreadyRegisteredWarning = "Sentry tracing middleware has already registered. This call to UseSentryTracing is unnecessary.";
    private const string UseSentryTracingKey = "__UseSentryTracing";
    private const string ShouldRegisterKey = "__ShouldRegisterSentryTracing";

    internal static bool IsSentryTracingRegistered(this IApplicationBuilder builder)
        => builder.Properties.ContainsKey(UseSentryTracingKey);
    internal static void StoreRegistrationDecision(this IApplicationBuilder builder, bool shouldRegisterSentryTracing)
        => builder.Properties[ShouldRegisterKey] = shouldRegisterSentryTracing;

    internal static bool ShouldRegisterSentryTracing(this IApplicationBuilder builder)
    {
        if (builder.Properties.ContainsKey(UseSentryTracingKey))
        {
            // It's already been registered
            return false;
        }
        if (builder.Properties.TryGetTypedValue(ShouldRegisterKey, out bool shouldRegisterSentryTracing))
        {
            return shouldRegisterSentryTracing;
        }
        return true;
    }

    internal static IApplicationBuilder UseSentryTracingInternal(this IApplicationBuilder builder)
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

    /// <summary>
    /// Adds Sentry's tracing middleware to the pipeline.
    /// Make sure to place this middleware after <c>UseRouting(...)</c>.
    /// </summary>
    public static IApplicationBuilder UseSentryTracing(this IApplicationBuilder builder)
    {
        if (!builder.IsSentryTracingRegistered())
        {
            return builder.UseSentryTracingInternal();
        }
        // Warn on multiple calls
        var log = builder.ApplicationServices.GetService<ILoggerFactory>()
            ?.CreateLogger<SentryTracingMiddleware>();
        log?.LogWarning(AlreadyRegisteredWarning);
        return builder;
    }
}
