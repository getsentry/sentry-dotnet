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
                sut.ConfigureScope(s => s.AddBreadcrumb("test"));
                Assert.Single(sut.ScopeManager.GetCurrent().Scope.Breadcrumbs);
            }

            Assert.Empty(sut.ScopeManager.GetCurrent().Scope.Breadcrumbs);
        }

        [Fact]
        public void PushAndLockScope_DoesNotAffectOuterScope()
        {
            var sut = _fixture.GetSut();

            sut.ConfigureScope(s => Assert.False(s.Locked));
            using (sut.PushAndLockScope())
            {
                sut.ConfigureScope(s => Assert.True(s.Locked));
            }
            sut.ConfigureScope(s => Assert.False(s.Locked));
        }
    }
}
