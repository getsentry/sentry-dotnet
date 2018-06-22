using Microsoft.AspNetCore.Hosting;
using Sentry.Testing;

namespace Sentry.AspNetCore.Tests
{
    public class AspNetSentrySdkTestFixture : SentrySdkTestFixture
    {
        public override void Build()
        {
            var sentry = FakeSentryServer.CreateServer();
            var sentryHttpClient = sentry.CreateClient();
            ConfigureBuilder = b => b.UseSentry(options =>
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
            });

            base.Build();
        }
    }
}
