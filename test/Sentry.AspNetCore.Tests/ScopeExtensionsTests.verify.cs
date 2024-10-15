using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Sentry.AspNetCore.Tests;

[UsesVerify]
public partial class ScopeExtensionsTests
{
    [Fact]
    public Task Populate_RouteData_SetToScope()
    {
        // Arrange
        const string controller = "Ctrl";
        const string action = "Actn";
        const string version = "1.1";
        var routeFeature = new RoutingFeature
        {
            RouteData = new RouteData
            {
                Values =
                {
                    { "controller", controller },
                    { "action", action },
                    { "version", version },
                }
            }
        };
        var features = new FeatureCollection();
        features.Set<IRoutingFeature>(routeFeature);
        _httpContext.Features.Returns(features);
        _httpContext.Request.Method.Returns("GET");

        // Act
        _sut.Populate(_httpContext, SentryAspNetCoreOptions);

        return Verify(_sut);
    }
}
