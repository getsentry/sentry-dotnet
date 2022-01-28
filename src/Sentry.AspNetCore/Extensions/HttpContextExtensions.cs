using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

#if !NETSTANDARD2_0
using Microsoft.AspNetCore.Http.Features;
#endif

namespace Sentry.AspNetCore.Extensions
{
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
            if (LegacyRouteFormat(context) is { } legacyFormat)
            {
                return legacyFormat;
            }

            var sentryRouteName = context.Features.Get<TransactionNameProvider>();

            return sentryRouteName?.Invoke(context);
        }

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
            return RouteHasMvcParameters(routePattern)
                ? builder.Append(ReplaceMvcParameters(routePattern, context)).ToString()
                : builder.Append(routePattern).ToString();
        }

        // Internal for testing.
        internal static string? LegacyRouteFormat(HttpContext context)
        {
            // Fallback for legacy .UseMvc().
            // Uses context.Features.Get<IRoutingFeature?>() under the hood and CAN be null,
            // despite the annotations claiming otherwise.
            var routeData = context.GetRouteData();

            var controller = routeData?.Values["controller"]?.ToString();
            var action = routeData?.Values["action"]?.ToString();
            var area = routeData?.Values["area"]?.ToString();

            if (!string.IsNullOrWhiteSpace(action))
            {
                var builder = new StringBuilder();
                if (context.Request.PathBase.HasValue)
                {
                    builder.Append(context.Request.PathBase.Value?.TrimStart('/'))
                        .Append('.');
                }

                if (!string.IsNullOrWhiteSpace(area))
                {
                    builder.Append(area)
                        .Append('.');
                }

                builder.Append(controller)
                    .Append('.')
                    .Append(action);
                return builder.ToString();
            }

            // If the handler doesn't use routing (i.e. it checks `context.Request.Path` directly),
            // then there is no way for us to extract anything that resembles a route template.
            return null;
        }

        // Internal for testing.
        internal static string ReplaceMvcParameters(string route, HttpContext? context)
        {
            // Return RouteData or Null, marking the HttpContext as nullable since the output doesn't
            // shows the nullable output.
            var routeData = context?.GetRouteData();

            if (routeData?.Values["controller"]?.ToString() is { } controller)
            {
                route = Regex.Replace(route, "{controller=[^}]+}", controller);
            }

            if (routeData?.Values["action"]?.ToString() is { } action)
            {
                route = Regex.Replace(route, "{action=[^}]+}", action);
            }

            if (routeData?.Values["area"]?.ToString() is { } area)
            {
                route = Regex.Replace(route, "{area=[^}]+}", area);
            }

            return route;
        }

        // Internal for testing.
        internal static bool RouteHasMvcParameters(string route)
            => route.Contains("{controller=") || route.Contains("{action=") || route.Contains("{area=");

        public static string? TryGetTransactionName(this HttpContext context)
        {
            var route = context.TryGetRouteTemplate();
            if (string.IsNullOrWhiteSpace(route))
            {
                return null;
            }

            var method = context.Request.Method.ToUpperInvariant();

            // e.g. "GET /pets/1"
            return $"{method} {route}";
        }
    }
}
