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

    [Fact]
    public void UseSentry_NoDsnProvided_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() => _webHostBuilder.UseSentry().Build());
    }

    [Fact]
    public void UseSentry_DisableDsnString_DisabledSdk()
    {
        _ = _webHostBuilder.UseSentry(Sentry.Constants.DisableSdkDsnValue)
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
