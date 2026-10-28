using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;
using Sentry.NLog;

// Configure NLog to send logs to Sentry. This only adds the Sentry target: the SDK is initialized by UseSentry below.
var sentryTarget = new SentryTarget(new SentryNLogOptions
{
    // Warn and higher are added as breadcrumbs (default is Info)
    MinimumBreadcrumbLevel = NLog.LogLevel.Warn,
    // Error and higher are sent as events (default is Error)
    MinimumEventLevel = NLog.LogLevel.Error,
});

var nlogConfig = new LoggingConfiguration();
nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ColoredConsoleTarget("console"));
// Every entry that reaches the Sentry target is sent as a structured log, so this rule sets the minimum level for logs.
// Info and higher are sent as logs. Breadcrumbs and events can only use entries that pass this rule.
nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, sentryTarget);
LogManager.Configuration = nlogConfig;

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

LogManager.Shutdown();
