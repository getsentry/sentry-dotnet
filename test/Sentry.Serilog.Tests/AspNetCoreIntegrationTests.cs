using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Sentry.Protocol;
using Sentry.Testing;
using Xunit;

namespace Sentry.Serilog.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class AspNetCoreIntegrationTests : AspNetSentrySdkTestFixture
    {

#if NETCOREAPP3_1_OR_GREATER
        [Fact]
        public async Task UnhandledException_MarkedAsUnhandled()
        {
            var expectedException = new Exception("test");
            var handler = new RequestHandler
            {
                Path = "/throw",
                Handler = _ => throw expectedException
            };

            Handlers = new[] { handler };
            Build();
            _ = await HttpClient.GetAsync(handler.Path);

            Assert.Contains(Events, e => e.Logger == "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");
            Assert.Collection(Events, @event => Assert.Collection(@event.SentryExceptions, x => Assert.False(x.Mechanism?.Handled)));
        }
#endif
    }
}
