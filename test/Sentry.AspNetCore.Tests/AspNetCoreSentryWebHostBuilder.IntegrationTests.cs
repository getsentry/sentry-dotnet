using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore.TestUtils;

namespace Sentry.AspNetCore.Tests;

[Collection(nameof(SentrySdkCollection))]
public class SentryWebHostBuilderExtensionsIntegrationTests : AspNetSentrySdkTestFixture
{
    private readonly IWebHostBuilder _webHostBuilder = WebHost.CreateDefaultBuilder()
        .UseStartup<Startup>();

    [Fact]
    public void UseSentry_ValidDsnString_EnablesSdk()
    {
        _ = _webHostBuilder.UseSentry(ValidDsn)
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

    [SkippableFact]
    public void UseSentry_NoDsnProvided_ThrowsException()
    {
#if SENTRY_DSN_DEFINED_IN_ENV
        Skip.If(true, "This test only works when the DSN is not configured as an environment variable.");
#endif
        Assert.Throws<ArgumentNullException>(() => _webHostBuilder.UseSentry().Build());
    }

    [SkippableFact]
    public void UseSentry_DisableDsnString_DisabledSdk()
    {
#if SENTRY_DSN_DEFINED_IN_ENV
        Skip.If(true, "This test only works when the DSN is not configured as an environment variable.");
#endif
        _ = _webHostBuilder.UseSentry(Sentry.SentryConstants.DisableSdkDsnValue)
            .Build();

        Assert.False(SentrySdk.IsEnabled);
    }

    [Fact]
    public void UseSentry_OptionsNotInitializeSdk_DisabledSdk()
    {
        _ = _webHostBuilder.UseSentry(o => o.InitializeSdk = false)
            .Build();

        Assert.False(SentrySdk.IsEnabled);
    }
}
