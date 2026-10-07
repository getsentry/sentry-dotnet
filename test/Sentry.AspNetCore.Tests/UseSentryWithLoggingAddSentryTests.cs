#if NET6_0_OR_GREATER
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sentry.Extensions.Logging;

namespace Sentry.AspNetCore.Tests;

// Calling both UseSentry and Logging.AddSentry is redundant, but reachable. Whichever registered the hub
// accessor first used to win, so calling AddSentry first left the SDK disabled.
[Collection(nameof(SentrySdkCollection))]
public class UseSentryWithLoggingAddSentryTests : IDisposable
{
    private readonly List<SentryEvent> _events = new();
    private readonly List<SentryLog> _logs = new();
    private readonly InMemoryDiagnosticLogger _diagnosticLogger = new();

    public void Dispose() => SentrySdk.Close();

    private WebApplication Build(bool useSentryFirst, bool enableLogs = false)
    {
        var builder = WebApplication.CreateBuilder();

        void UseSentry() => builder.WebHost.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.Dsn = ValidDsn;
            options.BackgroundWorker = Substitute.For<IBackgroundWorker>();
            options.AutoSessionTracking = false;
            options.InitNativeSdks = false;
            options.Debug = true;
            options.DiagnosticLogger = _diagnosticLogger;
            options.EnableLogs = enableLogs;
            options.SetBeforeSend((e, _) =>
            {
                _events.Add(e);
                return null;
            });
            options.SetBeforeSendLog(log =>
            {
                _logs.Add(log);
                return null;
            });
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

        return builder.Build();
    }

    private static ILogger CreateLogger(WebApplication app)
        => app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("test_category");

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentry_InitializesSdk(bool useSentryFirst)
    {
        using var app = Build(useSentryFirst);

        Assert.True(app.Services.GetRequiredService<IHub>().IsEnabled);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentry_CapturesEventAndBreadcrumbOnce(bool useSentryFirst)
    {
        using var app = Build(useSentryFirst);
        var logger = CreateLogger(app);

        logger.LogInformation("breadcrumb");
        logger.LogError("event");

        _events.Should().ContainSingle()
            .Which.Breadcrumbs.Should().ContainSingle(b => b.Message == "breadcrumb");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentryAndEnableLogs_SendsLogOnce(bool useSentryFirst)
    {
        using var app = Build(useSentryFirst, enableLogs: true);

        CreateLogger(app).LogWarning("log");

        _logs.Should().ContainSingle();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentry_WarnsCallIsRedundant(bool useSentryFirst)
    {
        using var app = Build(useSentryFirst);

        _ = app.Services.GetRequiredService<ILoggerFactory>();

        _diagnosticLogger.Entries.Should().ContainSingle(e =>
            e.Level == SentryLevel.Warning && e.Message == SentryLoggingOptions.RedundantWithHostIntegration);
    }
}
#endif
