using System;
using Microsoft.AspNetCore.Hosting;
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests
{
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
                options.Dsn = DsnSamples.ValidDsnWithSecret;
                options.SentryHttpClientFactory = new DelegateHttpClientFactory(o => sentryHttpClient);

                Configure?.Invoke(options);
            });

            AfterConfigureBuilder?.Invoke(builder);
        }
    }
}
