using System.Reflection;
using System.Web;
using Sentry.AspNet.Internal;

public class SystemWebVersionLocatorTests
{
    private class Fixture
    {
        public HttpContext HttpContext { get; set; }

        public Fixture()
        {
            HttpContext = HttpContextBuilder.Build();
        }

        public Assembly GetSut()
        {
            HttpContext.Current = HttpContext;
            HttpContext.Current.ApplicationInstance = new HttpApplication();
            return HttpContext.Current.ApplicationInstance.GetType().Assembly;
        }
    }

    private readonly Fixture _fixture = new();

    [Fact]
    public void GetCurrent_GetEntryAssemblyNull_HttpApplicationAssembly()
    {
        _fixture.HttpContext.ApplicationInstance = new HttpApplication();
        var sut = _fixture.GetSut();
        var expected = ApplicationVersionLocator.GetCurrent(sut);

        var actual = SystemWebVersionLocator.Resolve((string)null, _fixture.HttpContext);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetCurrent_GetEntryAssemblySet_HttpApplicationAssembly()
    {
        var expected = ApplicationVersionLocator.GetCurrent();

        var actual = SystemWebVersionLocator.Resolve(new SentryOptions(), _fixture.HttpContext);

        Assert.Equal(expected, actual);
    }
}
