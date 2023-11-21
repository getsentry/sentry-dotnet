#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore;
using Sentry.AspNetCore.TestUtils;

namespace Sentry.Serilog.Tests;

// Allows integration tests the include the background worker and mock only the HTTP bits
public class AspNetSentrySdkTestFixture : SentrySdkTestFixture
{
    protected Action<SentryAspNetCoreOptions> Configure;

    protected Action<WebHostBuilder> AfterConfigureBuilder;

    protected override void ConfigureBuilder(WebHostBuilder builder)
    {
        var sentry = FakeSentryServer.CreateServer();
        var sentryHttpClient = sentry.CreateClient();
        _ = builder.UseSentry(options =>
        {
            options.Dsn = ValidDsn;
            options.SentryHttpClientFactory = new DelegateHttpClientFactory(_ => sentryHttpClient);

            Configure?.Invoke(options);
        });

        AfterConfigureBuilder?.Invoke(builder);
    }
}
#endif
