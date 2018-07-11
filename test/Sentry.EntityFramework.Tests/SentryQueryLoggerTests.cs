using System.Linq;
using NSubstitute;
using Sentry.Protocol;
using Xunit;

namespace Sentry.EntityFramework.Tests
{
    public class SentryQueryLoggerTests
    {
        private class Fixture
        {
            public IHub Hub { get; set; } = Substitute.For<IHub>();
            public Scope Scope { get; } = new Scope(new SentryOptions());
            public SentryQueryLogger GetSut() => new SentryQueryLogger(Hub);

            public Fixture()
            {
                Hub.IsEnabled.Returns(true);
                Hub.ConfigureScope(Arg.Invoke(Scope));
            }
        }

        private readonly Fixture _fixture = new Fixture();

        [Fact]
        public void Log_QueryLogger_CaptureEvent()
        {
            var expected = new
            {
                Query = "Expected query string",
                Level = BreadcrumbLevel.Debug,
                Category = "Entity Framework"
            };

            var sut = _fixture.GetSut();

            sut.Log(expected.Query);

            var b = _fixture.Scope.Breadcrumbs.First();
            Assert.Equal(expected.Query, b.Message);
            Assert.Equal(expected.Category, b.Category);
            Assert.Equal(expected.Level, b.Level);
        }
    }
}
