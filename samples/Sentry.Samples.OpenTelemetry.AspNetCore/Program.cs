using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Sentry.Samples.OpenTelemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// OpenTelemetry Configuration
// See https://opentelemetry.io/docs/instrumentation/net/getting-started/
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            .AddSource(DiagnosticsConfig.ActivitySource.Name)
            .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSentry()
        );

builder.WebHost.UseSentry(options =>
{
    options.TracesSampleRate = 1.0;
    options.UseOpenTelemetry();
    options.Debug = builder.Environment.IsDevelopment();
});

var app = builder.Build();

var httpClient = new HttpClient();
app.MapGet("/hello", async context =>
{
    // Make an HTTP request to the /echo endpoint, to demonstrate that Baggage and TraceHeaders get propagated
    // correctly... in a real world situation, we might have received a request to this endpoint from an upstream
    // service that is instrumented with Sentry (passing in a SentryTraceHeader), and we might make an downstream
    // request to another service that's also instrumented with Sentry. Having a single TraceId that gets propagated
    // across all services by Sentry and OpenTelemetry ensures all of these events show as part of the same trace in
    // the performance dashboard in Sentry.
    var request = context.Request;
    if (request.Query.TryGetValue("topping", out var topping))
    {
        Activity.Current?.AddTag("topping", topping);
    }

    var url = $"{request.Scheme}://{request.Host}{request.PathBase}/echo";
    var result = await httpClient.GetStringAsync(url);
    await context.Response.WriteAsync(result);
});

app.MapGet("/echo", () => "Hi!");

app.MapGet("/throw", _ => throw new Exception("test"));

app.Run();
