#if NETCOREAPP3_1_OR_GREATER
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sentry.AspNetCore.Extensions;

namespace Sentry.AspNetCore.Tests.Extensions;

public class HttpContextExtensionsTests
{
    private static class Fixture
    {
        public static HttpContext GetSut(string pathBase = null)
        {
            var httpContext = new DefaultHttpContext();
            if (pathBase is not null)
            {
                // pathBase must start with '/' otherwise the new PathString will throw an exception.
                httpContext.Request.PathBase = new PathString(pathBase);
            }

            return httpContext;
        }

        public static HttpContext GetMvcSut(
            string area = null,
            string controller = null,
            string action = null,
            string pathBase = null,
            string version = null)
        {
            var httpContext = new DefaultHttpContext();
            if (pathBase is not null)
            {
                // pathBase must start with '/' otherwise the new PathString will throw an exception.
                httpContext.Request.PathBase = new PathString(pathBase);
            }

            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "controller", controller);
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "action", action);
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "area", area);
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "version", version);
            return httpContext;
        }

        private static void AddRouteValuesIfNotNull(RouteValueDictionary route, string key, string value)
        {
            if (value is not null)
            {
                route.Add(key, value);
            }
        }
    }

    [Fact]
    public void TryGetRouteTemplate_NoRoute_NullOutput()
    {
        // Arrange
        var httpContext = Fixture.GetSut();

        // Act
        var filteredRoute = httpContext.TryGetRouteTemplate();

        // Assert
        Assert.Null(filteredRoute);
    }

    [Fact]
    public void TryGetRouteTemplate_WithSentryRouteName_RouteName()
    {
        // Arrange
        var expectedName = "abc";
        var httpContext = Fixture.GetSut();
        httpContext.Features.Set((TransactionNameProvider)(_ => expectedName));

        // Act
        var filteredRoute = httpContext.TryGetCustomTransactionName();

        // Assert
        Assert.Equal(expectedName, filteredRoute);
    }
}
#endif
