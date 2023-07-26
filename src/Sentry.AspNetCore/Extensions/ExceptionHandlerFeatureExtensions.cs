using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing;

namespace Sentry.AspNetCore.Extensions;

internal static class ExceptionHandlerFeatureExtensions
{
#if !NET6_0_OR_GREATER
    internal static void ApplyRouteTags(this IExceptionHandlerFeature exceptionFeature, SentryEvent evt)
    {
    }

    internal static void ApplyTransactionName(this IExceptionHandlerFeature exceptionFeature, SentryEvent evt,
        string method)
    {
    }
#else
    internal static void ApplyRouteTags(this IExceptionHandlerFeature exceptionFeature, SentryEvent evt)
    {
        var endpoint = exceptionFeature.Endpoint as RouteEndpoint;

        var actionName = endpoint?.DisplayName;
        if (actionName is not null)
        {
            evt.SetTag("ActionName", actionName);
        }

        if (exceptionFeature.RouteValues is {} routeValues)
        {
            if (routeValues.TryGetValue("controller", out var controller))
            {
                evt.SetTag("route.controller", $"{controller}");
            }
            if (routeValues.TryGetValue("action", out var action))
            {
                evt.SetTag("route.action", $"{action}");
            }
        }
    }

    internal static void ApplyTransactionName(this IExceptionHandlerFeature exceptionFeature, SentryEvent evt,
        string method)
    {
        // If no route template details are available, fall back to the Path
        var route = exceptionFeature.TryGetRouteTemplate() ?? exceptionFeature.Path;
        if (!string.IsNullOrWhiteSpace(route))
        {
            evt.TransactionName = $"{method} {route}"; // e.g. "GET /pets/{id}"
        }
    }

    internal static string? TryGetRouteTemplate(this IExceptionHandlerFeature exceptionFeature)
    {
        // Requires .UseRouting()/.UseEndpoints()
        var endpoint = exceptionFeature.Endpoint as RouteEndpoint;
        var routePattern = endpoint?.RoutePattern.RawText;
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            return null;
        }

        var routeValues = exceptionFeature.RouteValues;
        if (routeValues is null)
        {
            return null;
        }

        if (RouteUtils.NewRouteFormat(routePattern, routeValues) is { } formattedRoute)
        {
            return formattedRoute;
        }

        return RouteUtils.LegacyRouteFormat(routeValues);
    }
#endif
}
