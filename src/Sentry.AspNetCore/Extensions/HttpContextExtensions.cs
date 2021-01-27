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
#if !NETSTANDARD2_0
            // Requires .UseRouting()/.UseEndpoints()
            var endpoint = context.Features.Get<IEndpointFeature?>()?.Endpoint as RouteEndpoint;
            var routePattern = endpoint?.RoutePattern.RawText;

            if (!string.IsNullOrWhiteSpace(routePattern))
            {
                return routePattern;
            }
#endif

            // Requires legacy .UseMvc()
            var routeData = context.Features.Get<IRoutingFeature?>()?.RouteData;
            var controller = routeData?.Values["controller"]?.ToString();
            var action = routeData?.Values["action"]?.ToString();
            var area = routeData?.Values["area"]?.ToString();

            if (!string.IsNullOrWhiteSpace(action))
            {
                return !string.IsNullOrWhiteSpace(area)
                    ? $"{area}.{controller}.{action}"
                    : $"{controller}.{action}";
            }

            // If the handler doesn't use routing (i.e. it checks `context.Request.Path` directly),
            // then there is no way for us to extract anything that resembles a route template.
            return null;
        }

        /// <summary>
        /// Get the route template of the current path, or the path string itself if there is no route template
        /// </summary>
        /// <param name="context"></param>
        /// <param name="usesPathName">Returns true if we return the path name instead of a route template</param>
        /// <returns></returns>
        public static string GetTransactionName(this HttpContext context, out bool usesPathName)
        {
            // Try to get the route template or fallback to the request path

            usesPathName = false;
            var method = context.Request.Method.ToUpperInvariant();
            var route = context.TryGetRouteTemplate();
            if (route == null)
            {
                usesPathName = true;
                route = context.Request.Path;
            }

            return $"{method} {route}";
        }
    }
}
