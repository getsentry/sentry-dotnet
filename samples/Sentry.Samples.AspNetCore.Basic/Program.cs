var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif

    // Enable Sentry performance monitoring
    options.TracesSampleRate = 1.0;

#if DEBUG
    // Log debug information about the Sentry SDK
    options.Debug = true;
#endif

    // This option enables Logs sent to Sentry.
    options.Experimental.EnableLogs = true;
});

var app = builder.Build();

// An example ASP.NET Core endpoint that throws an exception when serving a request to path: /throw
app.MapGet("/throw/{message?}", context =>
{
    var exceptionMessage = context.GetRouteValue("message") as string;

    var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
        .CreateLogger<Program>();

    log.LogInformation("Handling some request...");

    var hub = context.RequestServices.GetRequiredService<IHub>();
    hub.ConfigureScope(s =>
    {
        // More data can be added to the scope like this:
        s.SetTag("Sample", "ASP.NET Core"); // indexed by Sentry
        s.SetExtra("Extra!", "Some extra information");
    });

    log.LogInformation("Logging info...");
    log.LogWarning("Logging some warning!");

    // The following exception will be captured by the SDK and the event
    // will include the Log messages and any custom scope modifications
    // as exemplified above.
    throw new Exception(
        exceptionMessage ?? "An exception thrown from the ASP.NET Core pipeline");
});

// Demonstrates how to add tracing in custom middleware
app.Use(async (context, next) =>
{
    var span = SentrySdk.StartSpan("CustomMiddlewareSpan", "middleware");
    try
    {
        var log = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger<Program>();

        log.LogInformation("Just chilling for a bit...");
        await Task.Delay(TimeSpan.FromMilliseconds(50)); // Simulate some work
        span.Finish();
    }
    catch (Exception e)
    {
        span.Finish(e);
        throw;
    }
    await next();
});

app.Run();
