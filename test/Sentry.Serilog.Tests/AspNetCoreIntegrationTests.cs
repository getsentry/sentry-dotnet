using Sentry.Testing;

namespace Sentry.Serilog.Tests;

[Collection(nameof(SentrySdkCollection))]
public class AspNetCoreIntegrationTests : AspNetSentrySdkTestFixture
{
#if NETCOREAPP3_1_OR_GREATER
    [Fact]
    public async Task UnhandledException_MarkedAsUnhandled()
    {
        var handler = new RequestHandler
        {
            Path = "/throw",
            Handler = _ => throw new Exception("test")
        };

        Handlers = new[] { handler };
        Build();
        _ = await HttpClient.GetAsync(handler.Path);

        Assert.Contains(Events, e => e.Logger == "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");
        Assert.Collection(Events, @event => Assert.Collection(@event.SentryExceptions, x => Assert.False(x.Mechanism?.Handled)));
    }
#endif
}
