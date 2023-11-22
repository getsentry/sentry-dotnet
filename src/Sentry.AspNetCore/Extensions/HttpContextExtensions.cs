using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sentry.Extensibility;

using Microsoft.AspNetCore.Http.Features;

namespace Sentry.AspNetCore.Extensions;

internal static class HttpContextExtensions
{
    internal static string? TryGetRouteTemplate(this HttpContext context)
    {
        // Requires .UseRouting()/.UseEndpoints()
        var endpoint = context.Features.Get<IEndpointFeature?>()?.Endpoint as RouteEndpoint;
        var routePattern = endpoint?.RoutePattern.RawText;

        // GetRouteData can return null on netstandard2 (despite annotations claiming otherwise)
        if (RouteUtils.NewRouteFormat(routePattern, context.GetRouteData()?.Values, context.Request.PathBase)
            is { } formattedRoute)
        {
            return formattedRoute;
        }

        // Fallback for legacy .UseMvc().
        // Note: GetRouteData can return null on netstandard2
        return (context.GetRouteData() is { } routeData)
                ? RouteUtils.LegacyRouteFormat(routeData.Values, context.Request.PathBase)
                : null;
    }

    internal static string? TryGetCustomTransactionName(this HttpContext context) =>
        context.Features.Get<TransactionNameProvider>()?.Invoke(context);

    public static string? TryGetTransactionName(this HttpContext context)
    {
        var route = context.TryGetRouteTemplate();
        if (string.IsNullOrWhiteSpace(route))
        {
            return null;
        }

        var method = context.Request.Method.ToUpperInvariant();

        // e.g. "GET /pets/{id}"
        return $"{method} {route}";
    }

    public static SentryTraceHeader? TryGetSentryTraceHeader(this HttpContext context, SentryOptions? options)
    {
        var value = context.Request.Headers.GetValueOrDefault(SentryTraceHeader.HttpHeaderName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        options?.LogDebug("Received Sentry trace header '{0}'.", value);

        try
        {
            return SentryTraceHeader.Parse(value!);
        }
        catch (Exception ex)
        {
            options?.LogError(ex, "Invalid Sentry trace header '{0}'.", value);
            return null;
        }
    }

    public static BaggageHeader? TryGetBaggageHeader(this HttpContext context, SentryOptions? options)
    {
        var value = context.Request.Headers.GetValueOrDefault(BaggageHeader.HttpHeaderName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        // Note: If there are multiple baggage headers, they will be joined with comma delimiters,
        // and can thus be treated as a single baggage header.

        options?.LogDebug("Received baggage header '{0}'.", value);

        try
        {
            return BaggageHeader.TryParse(value!, onlySentry: true);
        }
        catch (Exception ex)
        {
            options?.LogError(ex, "Invalid baggage header '{0}'.", value);
            return null;
        }
    }
}
