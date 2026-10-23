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

    private WebApplication Build(bool useSentryFirst, bool enableLogs = false, string dsn = ValidDsn)
    {
        var builder = WebApplication.CreateBuilder();

        if (useSentryFirst)
        {
            UseSentry(builder, enableLogs, dsn);
            builder.Logging.AddSentry();
        }
        else
        {
            builder.Logging.AddSentry();
            UseSentry(builder, enableLogs, dsn);
        }

        return builder.Build();
    }

    private WebApplication BuildWithProvidersCleared(Action<SentryLoggingOptions> configureLogging = null)
    {
        var builder = WebApplication.CreateBuilder();

        UseSentry(builder);
        builder.Logging.ClearProviders();
        builder.Logging.AddSentry(configureLogging);

        return builder.Build();
    }

    private void UseSentry(WebApplicationBuilder builder, bool enableLogs = false, string dsn = ValidDsn)
        => builder.WebHost.UseSentry((SentryAspNetCoreOptions options) =>
        {
            options.Dsn = dsn;
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

    [Fact]
    public void UseSentry_ClearProvidersThenLoggingAddSentry_CapturesEventAndBreadcrumbOnce()
    {
        using var app = BuildWithProvidersCleared();
        var logger = CreateLogger(app);

        logger.LogInformation("breadcrumb");
        logger.LogError("event");

        _events.Should().ContainSingle()
            .Which.Breadcrumbs.Should().ContainSingle(b => b.Message == "breadcrumb");
    }

    [Fact]
    public void UseSentry_ClearProvidersThenLoggingAddSentry_IgnoresErrorsLoggedByExceptionHandler()
    {
        using var app = BuildWithProvidersCleared();
        _ = app.Services.GetRequiredService<IHub>();
        var exceptionHandlerLogger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware");

        exceptionHandlerLogger.LogError(new InvalidOperationException(), "handled by UseExceptionHandler");
        CreateLogger(app).LogError("logged by the app");

        _events.Should().ContainSingle().Which.Message!.Formatted.Should().Be("logged by the app");
    }

    [Fact]
    public void UseSentry_ClearProvidersThenLoggingAddSentry_DoesNotWarn()
    {
        using var app = BuildWithProvidersCleared();

        _ = app.Services.GetRequiredService<ILoggerFactory>();

        _diagnosticLogger.Entries.Should().NotContain(e =>
            e.Message == SentryLoggingOptions.RedundantWithHostIntegration
            || e.Message == SentryLoggingOptions.OptionsIgnoredWithHostIntegration);
    }

    [Fact]
    public void UseSentry_ClearProvidersThenLoggingAddSentryWithOptions_WarnsOptionsAreIgnored()
    {
        using var app = BuildWithProvidersCleared(options => options.MinimumEventLevel = LogLevel.Warning);

        _ = app.Services.GetRequiredService<ILoggerFactory>();

        _diagnosticLogger.Entries.Should().ContainSingle(e =>
            e.Level == SentryLevel.Warning && e.Message == SentryLoggingOptions.OptionsIgnoredWithHostIntegration);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UseSentry_WithLoggingAddSentryAndSdkDisabled_CreatesLogger(bool useSentryFirst)
    {
        using var app = Build(useSentryFirst, dsn: SentryConstants.DisableSdkDsnValue);

        var createLogger = () => CreateLogger(app);

        createLogger.Should().NotThrow();
    }
}
#endif
