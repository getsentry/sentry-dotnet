using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using NSubstitute;
using Sentry.AspNet.Internal;
using Sentry.Extensibility;
using Sentry.Internal;
using Xunit;

namespace Sentry.AspNet.Tests.Internal
{
    public class SystemWebVersionLocatorTests
    {
        private class Fixture
        {
            public HttpContext HttpContext { get; set; }

            public Fixture()
            {
                HttpContext = new HttpContext(new HttpRequest("test", "http://test", null), new HttpResponse(new StringWriter()));
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

            var actual = SystemWebVersionLocator.GetCurrent(null);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void GetCurrent_GetEntryAssemblySet_HttpApplicationAssembly()
        {
            var expected = ApplicationVersionLocator.GetCurrent();

            var actual = SystemWebVersionLocator.GetCurrent();

            Assert.Equal(expected, actual);
        }

    }
}
