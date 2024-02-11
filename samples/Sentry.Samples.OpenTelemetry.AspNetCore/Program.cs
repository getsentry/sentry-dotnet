using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.Samples.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Configuration
// See https://opentelemetry.io/docs/instrumentation/net/getting-started/
builder.Services.AddOpenTelemetry()
    // This block configures OpenTelemetry to send traces to Sentry
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            // Here we add a custom source we've created, which sends telemetry in the `LookupUser` method below
            .AddSampleInstrumentation()
            // Here we can optionally configure resource attributes that get sent with every trace
            .ConfigureResource(resource => resource.AddService(SampleTelemetry.ServiceName))
            // The two lines below take care of configuring sources for ASP.NET Core and HttpClient
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Finally we configure OpenTelemetry to send traces to Sentry
            .AddSentry()
    )
    // This block configures OpenTelemetry metrics that we care about... later we'll configure Sentry to capture these
    .WithMetrics(metrics =>
    {
        metrics
            .AddRuntimeInstrumentation() // <-- Requires the OpenTelemetry.Instrumentation.Runtime package
            // Collect some of the built-in ASP.NET Core metrics
            .AddMeter(
                "Microsoft.AspNetCore.Hosting",
                "Microsoft.AspNetCore.Server.Kestrel",
                "System.Net.Http");
    });

builder.WebHost.UseSentry(options =>
{
    options.Dsn = "...Your DSN...";
    options.Debug = builder.Environment.IsDevelopment();
    options.SendDefaultPii = true;
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
    // This shows experimental support for capturing OpenTelemetry metrics with Sentry
    options.ExperimentalMetrics = new ExperimentalMetricsOptions()
    {
        // Here we're telling Sentry to capture all built-in metrics. This includes all the metrics we configured
        // OpenTelemetry to emit when we called `builder.Services.AddOpenTelemetry()` above:
        // - "OpenTelemetry.Instrumentation.Runtime"
        // - "Microsoft.AspNetCore.Hosting",
        // - "Microsoft.AspNetCore.Server.Kestrel",
        // - "System.Net.Http"
        CaptureSystemDiagnosticsMeters = BuiltInSystemDiagnosticsMeters.All
    };
});

builder.Services
    .AddAuthorization()
    .AddAuthentication(FakeAuthHandler.AuthenticationScheme)
    .AddScheme<AuthenticationSchemeOptions, FakeAuthHandler>(FakeAuthHandler.AuthenticationScheme, _ => { });

var app = builder.Build();
app.UseAuthorization();

var httpClient = new HttpClient();
app.MapGet("/hello", async context =>
{
    var request = context.Request;
    var name = LookupUser(request);
    Activity.Current?.AddTag("name", name);

    // Make an HTTP request to the /echo endpoint, to demonstrate that Baggage and TraceHeaders get propagated
    // correctly... in a real world situation, we might have received a request to this endpoint from an upstream
    // service that is instrumented with Sentry (passing in a SentryTraceHeader), and we might make an downstream
    // request to another service that's also instrumented with Sentry. Having a single TraceId that gets propagated
    // across all services by Sentry and OpenTelemetry ensures all of these events show as part of the same trace in
    // the performance dashboard in Sentry.
    var url = $"{request.Scheme}://{request.Host}{request.PathBase}/echo/{name}";
    var result = await httpClient.GetStringAsync(url);
    await context.Response.WriteAsync(result);
});

app.MapGet("/echo/{name}", (string name) => $"Hi {name}!");

app.MapGet("/private", async context =>
{
    var user = context.User;
    var result = $"Hello {user.Identity?.Name}";
    await context.Response.WriteAsync(result);
}).RequireAuthorization();

app.MapGet("/throw", _ => throw new Exception("test"));

app.Run();

static string LookupUser(HttpRequest request)
{
    using var _ = SampleTelemetry.ActivitySource.StartActivity(nameof(LookupUser));
    Thread.Sleep(100); // Simulate some work
    return (request.Query.TryGetValue("name", out var name))
        ? name.ToString()
        : "stranger";
}
