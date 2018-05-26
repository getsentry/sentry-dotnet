using Sentry.Internals;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class SdkTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();

            public Sdk GetSut() => new Sdk(SentryOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
        {
            var sut = _fixture.GetSut();

            using (sut.PushScope())
            {
                sut.ConfigureScope(s => s.AddBreadcrumb(new Breadcrumb("test", "unit")));
                Assert.Single(sut.ScopeManagement.GetCurrent().Breadcrumbs);
            }

            Assert.Empty(sut.ScopeManagement.GetCurrent().Breadcrumbs);
        }
    }
}
