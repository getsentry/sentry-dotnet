using System;
using System.Threading.Tasks;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentryCoreDependentCollection))]
    public class IntegrationsTests
    {
        private readonly SentryCoreDependentCollection _fixture;

        public IntegrationsTests(SentryCoreDependentCollection fixture)
        {
            fixture.Build();
            _fixture = fixture;
        }

        [Fact]
        public async Task UnhandledException_AvailableThroughLastExceptionFilter()
        {
            var expectedException = new Exception("test");
            var handler = new RequestHandler
            {
                Path = "/throw",
                Handler = context => throw expectedException
            };

            _fixture.Handlers = new[] { handler };
            await _fixture.HttpClient.GetAsync(handler.Path);

            Assert.Same(expectedException, _fixture.LastExceptionFilter.LastException);
        }
    }
}
