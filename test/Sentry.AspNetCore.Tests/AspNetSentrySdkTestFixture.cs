using System;
using Microsoft.AspNetCore.Hosting;
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests
{
    public class AspNetSentrySdkTestFixture : SentrySdkTestFixture
    {
        protected Action<SentryAspNetCoreOptions> Configure;

        protected Action<WebHostBuilder> AfterConfigureBuilder;

        protected override void ConfigureBuilder(WebHostBuilder builder)
        {
            var sentry = FakeSentryServer.CreateServer();
            var sentryHttpClient = sentry.CreateClient();
            builder.UseSentry(options =>
            {
                options.Dsn = DsnSamples.ValidDsnWithSecret;
                options.Init(i =>
                {
                    i.Http(h =>
                    {
                        h.SentryHttpClientFactory = new DelegateHttpClientFactory((d, o)
                            => sentryHttpClient);
                    });
                });

                Configure?.Invoke(options);
            });

            AfterConfigureBuilder?.Invoke(builder);
        }
    }
}
