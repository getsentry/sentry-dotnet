using Microsoft.AspNetCore.Routing;

namespace Sentry.AspNetCore;

internal static class RouteUtils
{
    // Internal for testing.
    internal static string? NewRouteFormat(string? routePattern, RouteValueDictionary? values, string? pathBase = null)
    {
        if (string.IsNullOrWhiteSpace(routePattern))
        {
            return null;
        }

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            builder.Append(pathBase.TrimStart('/'))
                .Append('/');
        }

        // Skip route pattern if it resembles to a MVC route or null  e.g.
        // {controller=Home}/{action=Index}/{id?}
        if (RouteHasMvcParameters(routePattern))
        {
            builder.Append(ReplaceMvcParameters(routePattern, values));
        }
        else
        {
            if (builder is { Length: > 1 } && builder[^1].Equals('/') && routePattern[0] == '/')
            {
                builder.Length--;
            }

            builder.Append(routePattern);
        }

        // Force a leading slash (if there isn't already one present)
        return $"/{builder.ToString().TrimStart('/')}";
    }

    // Internal for testing.
    internal static string? LegacyRouteFormat(RouteValueDictionary values, string? pathBase = null)
    {
        if (values["action"] is not string action)
        {
            // If the handler doesn't use routing (i.e. it checks `context.Request.Path` directly),
            // then there is no way for us to extract anything that resembles a route template.
            return null;
        }

        var builder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(pathBase))
        {
            builder.Append(pathBase.TrimStart('/'))
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
    internal static string ReplaceMvcParameters(string route, RouteValueDictionary? values)
    {
        // GetRouteData can return null on netstandard2
        if (values == null)
        {
            return route;
        }

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
}
