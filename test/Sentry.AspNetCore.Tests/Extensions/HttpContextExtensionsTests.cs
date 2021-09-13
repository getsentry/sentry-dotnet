#if !NETCOREAPP2_1
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sentry.AspNetCore.Extensions;
using Xunit;

namespace Sentry.AspNetCore.Tests.Extensions
{
    public class HttpContextExtensionsTests
    {
        private static void AddRouteValuesIfNotNull(RouteValueDictionary route, string key, string value)
        {
            if (value is not null)
            {
                route.Add(key, value);
            }
        }

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
        public void ReplaceMcvParameters_ParsedParameters(string routeInput, string assertOutput, string context, string action, string area)
        {
            // Arrange
            var httpContext = new DefaultHttpContext();
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "controller", context);
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "action", action);
            AddRouteValuesIfNotNull(httpContext.Request.RouteValues, "area", area);

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
    }
}
#endif
