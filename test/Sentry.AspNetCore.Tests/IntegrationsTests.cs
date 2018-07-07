using System;
using System.Threading.Tasks;
using Sentry.Testing;
using Xunit;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public partial class IntegrationsTests : AspNetSentrySdkTestFixture
    {
        [Fact]
        public async Task UnhandledException_AvailableThroughLastExceptionFilter()
        {
            var expectedException = new Exception("test");
            var handler = new RequestHandler
            {
                Path = "/throw",
                Handler = context => throw expectedException
            };

            Handlers = new[] { handler };
            Build();
            await HttpClient.GetAsync(handler.Path);

            Assert.Same(expectedException, LastExceptionFilter.LastException);
        }
    }
}
