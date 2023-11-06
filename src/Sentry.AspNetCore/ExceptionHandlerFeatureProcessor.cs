using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Routing;
using Sentry.Extensibility;

namespace Sentry.AspNetCore;

#if NET6_0_OR_GREATER
internal class ExceptionHandlerFeatureProcessor : ISentryEventExceptionProcessor
{
    private readonly string _originalMethod;
    private readonly IExceptionHandlerFeature _exceptionHandlerFeature;

    public ExceptionHandlerFeatureProcessor(string originalMethod, IExceptionHandlerFeature exceptionHandlerFeature)
    {
        _originalMethod = originalMethod;
        _exceptionHandlerFeature = exceptionHandlerFeature;
    }

    public void Process(Exception exception, SentryEvent sentryEvent)
    {
        // When exceptions get caught by the UseExceptionHandler feature we reset the TransactionName and Tags
        // to reflect the route values of the original route (not the global error handling route)
        ApplyTransactionName(sentryEvent, _originalMethod);
        ApplyRouteTags(sentryEvent);
    }

    internal void ApplyRouteTags(SentryEvent evt)
    {
        var endpoint = _exceptionHandlerFeature.Endpoint as RouteEndpoint;

        var actionName = endpoint?.DisplayName;
        if (actionName is not null)
        {
            evt.Tags["ActionName"] = actionName;
        }

        if (_exceptionHandlerFeature.RouteValues is {} routeValues)
        {
            if (routeValues.TryGetValue("controller", out var controller))
            {
                evt.Tags["route.controller"] = $"{controller}";
            }
            if (routeValues.TryGetValue("action", out var action))
            {
                evt.Tags["route.action"] = $"{action}";
            }
        }
    }

    internal void ApplyTransactionName(SentryEvent evt,
        string method)
    {
        // If no route template details are available, fall back to the Path
        var route = TryGetRouteTemplate() ?? _exceptionHandlerFeature.Path;
        if (!string.IsNullOrWhiteSpace(route))
        {
            evt.TransactionName = $"{method} {route}"; // e.g. "GET /pets/{id}"
        }
    }

    internal string? TryGetRouteTemplate()
    {
        // Requires .UseRouting()/.UseEndpoints()
        var endpoint = _exceptionHandlerFeature.Endpoint as RouteEndpoint;
        var routePattern = endpoint?.RoutePattern.RawText;
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            return null;
        }

        var routeValues = _exceptionHandlerFeature.RouteValues;
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
}
#endif
