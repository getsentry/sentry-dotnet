using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Web;

// Configure NLog to send logs to Sentry. This only adds the Sentry target: the SDK is initialized by UseSentry below.
var nlogConfig = new LoggingConfiguration();
nlogConfig.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, new ColoredConsoleTarget("console"));
nlogConfig.AddSentry();
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
    logger.LogInformation("Stored as a breadcrumb");
    logger.LogError("Sent to Sentry as a single event");
    return "Logged an error through ILogger. Check Sentry for the event.";
});

app.MapGet("/throw", () => { throw new Exception("An exception thrown from the ASP.NET Core pipeline"); });

app.Run();

LogManager.Shutdown();
