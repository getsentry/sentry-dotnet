#if !NETCOREAPP2_1
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

    private static string LegacyFormat(string controller, string action, string area)
        => !string.IsNullOrWhiteSpace(area) ? $"{area}.{controller}.{action}" : $"{controller}.{action}";

    [Theory]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "theArea/house/about/{id?}", "house", "about", "theArea")]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "{area=MyArea}/house/about/{id?}", "house", "about", null)]
    [InlineData("{area=}/{controller=}/{action=}/{id?}", "{area=}/{controller=}/{action=}/{id?}", "house", "about", "theArea")]
    [InlineData("{controller=Home}/{action=Index}/{id?}", "house/about/{id?}", "house", "about", null)]
    [InlineData("{controller=Home}/{action=Index}", "house/about", "house", "about", null)]
    [InlineData("{controller=Home}/{id?}", "house/{id?}", "house", "about", null)]
    [InlineData("{action=Index}/{id?}", "about/{id?}", null, "about", null)]
    [InlineData("not/mvc/", "not/mvc/", "house", "about", "area")]
    [InlineData("not/mvc/{controller}/{action}/{area}", "not/mvc/{controller}/{action}/{area}", "house", "about", "area")]
    public void ReplaceMcvParameters_ParsedParameters(string routeInput, string assertOutput, string controller, string action, string area)
    {
        // Arrange
        var httpContext = Fixture.GetMvcSut(area, controller, action);
        // Act
        var filteredRoute = HttpContextExtensions.ReplaceMvcParameters(routeInput, httpContext);

        // Assert
        Assert.Equal(assertOutput, filteredRoute);
    }

    [Theory]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}")]
    [InlineData("{controller=Home}/{action=Index}/{id?}")]
    [InlineData("{controller=Home}/{id?}")]
    [InlineData("abc/{controller=}/")]
    [InlineData("{action=Index}/{id?}")]
    [InlineData("{area=Index}/{id?}")]
    [InlineData("v{version:apiVersion}/Target")]
    public void RouteHasMvcParameters_RouteWithMvcParameters_True(string route)
    {
        // Assert
        Assert.True(HttpContextExtensions.RouteHasMvcParameters(route));
    }

    [Theory]
    [InlineData("test/{area}")]
    [InlineData("test/{action}")]
    [InlineData("test/{controller}")]
    [InlineData("area/test")]
    [InlineData("/area/test")]
    [InlineData("/")]
    [InlineData("")]
    public void RouteHasMvcParameters_RouteWithoutMvcParameters_False(string route)
    {
        // Assert
        Assert.False(HttpContextExtensions.RouteHasMvcParameters(route));
    }

    [Theory]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "myPath/theArea/house/about/{id?}", "house", "about", "theArea")]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "myPath/{area=MyArea}/house/about/{id?}", "house", "about", null)]
    [InlineData("{area=}/{controller=}/{action=}/{id?}", "myPath/{area=}/{controller=}/{action=}/{id?}", "house", "about", "theArea")]
    [InlineData("{controller=Home}/{action=Index}/{id?}", "myPath/house/about/{id?}", "house", "about", null)]
    [InlineData("{controller=Home}/{action=Index}", "myPath/house/about", "house", "about", null)]
    [InlineData("{controller=Home}/{id?}", "myPath/house/{id?}", "house", "about", null)]
    [InlineData("{action=Index}/{id?}", "myPath/about/{id?}", null, "about", null)]
    public void NewRouteFormat_MvcRouteWithPathBase_ParsedParameters(string routeInput, string expectedOutput, string controller, string action, string area)
    {
        // Arrange
        var httpContext = Fixture.GetMvcSut(area, controller, action, pathBase: "/myPath");

        // Act
        var filteredRoute = HttpContextExtensions.NewRouteFormat(routeInput, httpContext);

        // Assert
        Assert.Equal(expectedOutput, filteredRoute);
    }

    [Theory]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "theArea/house/about/{id?}", "house", "about", "theArea", null)]
    [InlineData("{area=MyArea}/{controller=Home}/{action=Index}/{id?}", "{area=MyArea}/house/about/{id?}", "house", "about", null, null)]
    [InlineData("{area=}/{controller=}/{action=}/{id?}", "{area=}/{controller=}/{action=}/{id?}", "house", "about", "theArea", null)]
    [InlineData("{controller=Home}/{action=Index}/{id?}", "house/about/{id?}", "house", "about", null, null)]
    [InlineData("{controller=Home}/{action=Index}", "house/about", "house", "about", null, null)]
    [InlineData("{controller=Home}/{id?}", "house/{id?}", "house", "about", null, null)]
    [InlineData("{action=Index}/{id?}", "about/{id?}", null, "about", null, null)]
    [InlineData("v{version:apiVersion}/Target", "v1.1/Target", null, "about", null, "1.1")]
    public void NewRouteFormat_MvcRouteWithoutPathBase_ParsedParameters(string routeInput, string expectedOutput, string controller, string action, string area, string version)
    {
        // Arrange
        var httpContext = Fixture.GetMvcSut(area, controller, action, null, version);

        // Act
        var filteredRoute = HttpContextExtensions.NewRouteFormat(routeInput, httpContext);

        // Assert
        Assert.Equal(expectedOutput, filteredRoute);
    }

    [Theory]
    [InlineData("myPath/some/Path", "/myPath", "some/Path")]
    [InlineData("some/Path", null, "some/Path")]
    [InlineData(null, null, "")]
    [InlineData(null, null, null)]
    public void NewRouteFormat_WithPathBase_MatchesExpectedRoute(string expectedRoute, string pathBase, string rawRoute)
    {
        // Arrange
        var httpContext = Fixture.GetSut(pathBase);

        // Act
        var filteredRoute = HttpContextExtensions.NewRouteFormat(rawRoute, httpContext);

        // Assert
        Assert.Equal(expectedRoute, filteredRoute);
    }

    [Theory]
    [InlineData("myPath.myArea.myController.myAction", "/myPath", "myController", "myAction", "myArea")]
    [InlineData("myArea.myController.myAction", null, "myController", "myAction", "myArea")]
    [InlineData("myController.myAction", null, "myController", "myAction", null)]
    [InlineData(null, null, null, null, null)]
    public void LegacyRouteFormat_WithPathBase_MatchesExpectedRoute(string expectedRoute, string pathBase, string controller, string action, string area)
    {
        // Arrange
        var httpContext = Fixture.GetMvcSut(area, controller, action, pathBase);

        // Act
        var filteredRoute = HttpContextExtensions.LegacyRouteFormat(httpContext);

        // Assert
        Assert.Equal(expectedRoute, filteredRoute);
    }

    [Theory]
    [InlineData("myController", "myAction", "myArea")]
    [InlineData("myController", "myAction", null)]
    public void LegacyRouteFormat_ValidRoutes_MatchPreviousImplementationResult(string controller, string action, string area)
    {
        // Arrange
        var httpContext = Fixture.GetMvcSut(area, controller, action);

        // Act
        var filteredRoute = HttpContextExtensions.LegacyRouteFormat(httpContext);

        // Assert
        Assert.Equal(LegacyFormat(controller, action, area), filteredRoute);
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
        TransactionNameProvider sentryRouteName = _ => expectedName;
        var httpContext = Fixture.GetSut();
        httpContext.Features.Set(sentryRouteName);

        // Act
        var filteredRoute = httpContext.TryGetRouteTemplate();

        // Assert
        Assert.Equal(expectedName, filteredRoute);
    }
}
#endif
