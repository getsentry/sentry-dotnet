using Sentry.Internal;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests
{
    public class HubTests
    {
        private class Fixture
        {
            public SentryOptions SentryOptions { get; set; } = new SentryOptions();

            public Fixture() => SentryOptions.Dsn = new Dsn(DsnSamples.ValidDsnWithoutSecret);

            public Hub GetSut() => new Hub(SentryOptions);
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void PushScope_BreadcrumbWithinScope_NotVisibleOutside()
        {
            var sut = _fixture.GetSut();

            using (sut.PushScope())
            {
                sut.ConfigureScope(s => s.AddBreadcrumb(new Breadcrumb("test", "unit")));
                Assert.Single(sut.ScopeManagement.GetCurrent().Scope.Breadcrumbs);
            }

            Assert.Empty(sut.ScopeManagement.GetCurrent().Scope.Breadcrumbs);
        }
    }
}
