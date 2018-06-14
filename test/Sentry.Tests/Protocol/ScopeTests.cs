using System.Linq;
using NSubstitute;
using Sentry.Protocol;
using Xunit;

namespace Sentry.Tests.Protocol
{
    public class ScopeTests
    {
        [Fact]
        public void AddBreadcrumb()
        {
            const int limit = 5;
            var options = Substitute.For<IScopeOptions>();
            options.MaxBreadcrumbs.Returns(limit);

            var scope = new Scope(options);

            for (var i = 0; i < limit + 1; i++)
            {
                scope.AddBreadcrumb(i.ToString(), "test");
            }

            // Breadcrumb 0 is dropped
            Assert.Equal("1", scope.Breadcrumbs.First().Message);
            Assert.Equal("5", scope.Breadcrumbs.Last().Message);
        }
    }
}
