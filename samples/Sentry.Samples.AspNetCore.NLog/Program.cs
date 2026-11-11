using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

// UseSentry below only starts Sentry once the app is running. Initializing it here as well captures crashes during
// startup, until UseSentry takes over: https://docs.sentry.io/platforms/dotnet/guides/nlog/#capturing-startup-crashes
using var _ = SentrySdk.Init(o =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    o.Dsn = SamplesShared.Dsn;
#else
    o.Dsn = EnvironmentVariables.Dsn;
#endif
});

var nlogConfig = new LoggingConfiguration();
nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ColoredConsoleTarget("console"));
// Configure NLog to send logs to Sentry. This only adds the Sentry target: it does not initialize Sentry.
nlogConfig.AddSentry(options =>
    {
        // Warn and higher are added as breadcrumbs (default is Info)
        options.MinimumBreadcrumbLevel = NLog.LogLevel.Warn;
        // Error and higher are sent as events (default is Error)
        options.MinimumEventLevel = NLog.LogLevel.Error;
    },
    // Info and higher are sent as logs. Breadcrumbs and events can only use entries at this level or higher.
    minLevel: NLog.LogLevel.Info);
LogManager.Configuration = nlogConfig;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Send ILogger entries through NLog. ReplaceLoggerFactory stops the ILogger providers that UseSentry adds from also
    // capturing every entry, so each one reaches Sentry once, through the NLog target, whatever order these calls are in.
    builder.Host.UseNLog(new NLogAspNetCoreOptions { ReplaceLoggerFactory = true });

    builder.UseSentry(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#else
        options.Dsn = EnvironmentVariables.Dsn;
#endif
    });

    var app = builder.Build();

    app.MapGet("/", (ILogger<Program> logger) =>
    {
        logger.LogDebug("Below the Sentry rule's minimum level, so not sent to Sentry");
        logger.LogInformation("Sent as a log");
        logger.LogWarning("Sent as a log and added as a breadcrumb");
        logger.LogError("Sent as a log and as an event, which includes the breadcrumb above");
        return "Logged through ILogger at each level. Check Sentry for the logs and the event.";
    });

    app.MapGet("/throw", () => { throw new Exception("An exception thrown from the ASP.NET Core pipeline"); });

    app.Run();
    return 0;
}
catch (Exception ex)
{
    LogManager.GetCurrentClassLogger().Fatal(ex, "Application terminated unexpectedly");
    return 1;
}
finally
{
    LogManager.Shutdown();
}
