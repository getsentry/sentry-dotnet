#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.AspNetCore.Tests;

// Calling both UseSentry and Logging.AddSentry is redundant, but reachable. Whichever registered the hub
// accessor first used to win, so calling AddSentry first left the SDK disabled.
[Collection(nameof(SentrySdkCollection))]
public class UseSentryWithLoggingAddSentryTests : IDisposable
{
    public void Dispose() => SentrySdk.Close();

    private static IHub BuildHub(bool useSentryFirst)
    {
        var builder = WebApplication.CreateBuilder();

        void UseSentry() => builder.WebHost.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.Dsn = ValidDsn;
            options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            options.AutoSessionTracking = false;
            options.InitNativeSdks = false;
        });

        if (useSentryFirst)
        {
            UseSentry();
            builder.Logging.AddSentry();
        }
        else
        {
            builder.Logging.AddSentry();
            UseSentry();
        }

        using var app = builder.Build();
        return app.Services.GetRequiredService<IHub>();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentry_InitializesSdk(bool useSentryFirst)
        => Assert.True(BuildHub(useSentryFirst).IsEnabled);
}
#endif
