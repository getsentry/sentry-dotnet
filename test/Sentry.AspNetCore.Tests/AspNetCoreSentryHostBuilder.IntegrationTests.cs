using Microsoft.Extensions.Hosting;

namespace Sentry.AspNetCore.Tests
{
    [Collection(nameof(SentrySdkCollection))]
    public class SentryHostBuilderExtensionsIntegrationTests : AspNetSentrySdkTestFixture
    {
        private readonly IHostBuilder _hostBuilder = new HostBuilder();

        [Fact]
        public void UseSentry_ValidDsnString_EnablesSdk()
        {
            _ = _hostBuilder.UseSentry(DsnSamples.ValidDsnWithoutSecret)
                    .Build();

            try
            {
                Assert.True(SentrySdk.IsEnabled);
            }
            finally
            {
                SentrySdk.Close();
            }
        }

        [Fact]
        public void UseSentry_NoDsnProvided_DisabledSdk()
        {
            _ = _hostBuilder.UseSentry().Build();

            Assert.False(SentrySdk.IsEnabled);
        }

        [Fact]
        public void UseSentry_DisableDsnString_DisabledSdk()
        {
            _ = _hostBuilder.UseSentry(Sentry.Constants.DisableSdkDsnValue)
                    .Build();

            Assert.False(SentrySdk.IsEnabled);
        }

        [Fact]
        public void UseSentry_OptionsNotInitializeSdk_DisabledSdk()
        {
            _ = _hostBuilder.UseSentry(o => o.InitializeSdk = false)
                    .Build();

            Assert.False(SentrySdk.IsEnabled);
        }

    }
}
