using System.Web;
using Sentry.AspNet.Internal;

namespace Sentry.AspNet.Tests.Internal;

public class SystemWebVersionLocatorTests :
    HttpContextTest
{
    [Fact]
    public void GetCurrent_GetEntryAssemblyNull_HttpApplicationAssembly()
    {
        var expected = ApplicationVersionLocator.GetCurrent(typeof(HttpApplication).Assembly);

        var actual = SystemWebVersionLocator.Resolve((string)null, Context);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Null_HttpContext()
    {
        var actual = SystemWebVersionLocator.Resolve((string)null, null);

        Assert.Null(actual);
    }

    [Fact]
    public void HttpApplicationAssembly_VersionParsing()
    {
        var expected = ApplicationVersionLocator.GetCurrent(typeof(HttpApplication).Assembly);

        var actual = SystemWebVersionLocator.Resolve(Context);

        Assert.Equal(expected, actual);
    }
}
