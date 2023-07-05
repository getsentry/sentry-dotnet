using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using Sentry.Extensibility;

#if !NETSTANDARD2_0
using Microsoft.AspNetCore.Http.Features;
#endif

namespace Sentry.AspNetCore.Extensions;

internal static class HttpContextExtensions
{
    public static string? TryGetRouteTemplate(this HttpContext context)
    {
#if !NETSTANDARD2_0 // endpoint routing is only supported after ASP.NET Core 3.0
        // Requires .UseRouting()/.UseEndpoints()
        var endpoint = context.Features.Get<IEndpointFeature?>()?.Endpoint as RouteEndpoint;
        var routePattern = endpoint?.RoutePattern.RawText;

        if (NewRouteFormat(routePattern, context) is { } formattedRoute)
        {
            return formattedRoute;
        }
#endif
        return LegacyRouteFormat(context);
    }

    public static string? TryGetCustomTransactionName(this HttpContext context) =>
        context.Features.Get<TransactionNameProvider>()?.Invoke(context);

    // Internal for testing.
    internal static string? NewRouteFormat(string? routePattern, HttpContext context)
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            return null;
        }

        var builder = new StringBuilder();
        if (context.Request.PathBase.HasValue)
        {
            builder.Append(context.Request.PathBase.Value?.TrimStart('/'))
                .Append('/');
        }

        // Skip route pattern if it resembles to a MVC route or null  e.g.
        // {controller=Home}/{action=Index}/{id?}
        if (RouteHasMvcParameters(routePattern))
        {
            builder.Append(ReplaceMvcParameters(routePattern, context));
        }
        else
        {
            if (builder is { Length: > 1 } && builder[^1].Equals('/') && routePattern[0] == '/')
            {
                builder.Length--;
            }

            builder.Append(routePattern);
        }

        return builder.ToString();
    }

    // Internal for testing.
    internal static string? LegacyRouteFormat(HttpContext context)
    {
        // Fallback for legacy .UseMvc().
        // Uses context.Features.Get<IRoutingFeature?>() under the hood and CAN be null,
        // despite the annotations claiming otherwise.
        var routeData = context.GetRouteData();

        // GetRouteData can return null on netstandard2
        if (routeData == null)
        {
            return null;
        }

        var values = routeData.Values;

        if (values["action"] is not string action)
        {
            // If the handler doesn't use routing (i.e. it checks `context.Request.Path` directly),
            // then there is no way for us to extract anything that resembles a route template.
            return null;
        }

        var builder = new StringBuilder();
        if (context.Request.PathBase.HasValue)
        {
            builder.Append(context.Request.PathBase.Value?.TrimStart('/'))
                .Append('.');
        }

        if (values["area"] is string area)
        {
            builder.Append(area)
                .Append('.');
        }

        if (values["controller"] is string controller)
        {
            builder.Append(controller)
                .Append('.');
        }

        builder.Append(action);

        return builder.ToString();
    }

    // Internal for testing.
    internal static string ReplaceMvcParameters(string route, HttpContext context)
    {
        var routeData = context.GetRouteData();

        // GetRouteData can return null on netstandard2
        if (routeData == null)
        {
            return route;
        }

        var values = routeData.Values;

        if (values["controller"] is string controller)
        {
            route = Regex.Replace(route, "{controller=[^}]+}", controller);
        }

        if (values["action"] is string action)
        {
            route = Regex.Replace(route, "{action=[^}]+}", action);
        }

        if (values["area"] is string area)
        {
            route = Regex.Replace(route, "{area=[^}]+}", area);
        }

        if (values["version"] is string version)
        {
            route = Regex.Replace(route, "{version:[^}]+}", version);
        }

        return route;
    }

    // Internal for testing.
    internal static bool RouteHasMvcParameters(string route)
        => route.Contains("{controller=") ||
           route.Contains("{action=") ||
           route.Contains("{version:") ||
           route.Contains("{area=");

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
            return SentryTraceHeader.Parse(value);
        }
        catch (Exception ex)
        {
            options?.LogError("Invalid Sentry trace header '{0}'.", ex, value);
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
            return BaggageHeader.TryParse(value, onlySentry: true);
        }
        catch (Exception ex)
        {
            options?.LogError("Invalid baggage header '{0}'.", ex, value);
            return null;
        }
    }
}
