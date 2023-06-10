using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry;
using Sentry.OpenTelemetry;
using Sentry.Samples.AspNetCore.OpenTelemetry;

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
            .AddProcessor<SentrySpanProcessor>());

// Use the Sentry propagator to ensure sentry-trace and baggage headers are propagated correctly.
OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new SentryPropagator());

// You can use other propagators via composition, if needed.
// For example, if you need both Sentry and W3C Trace Context propagation, then you can do the following:

// OpenTelemetry.Sdk.SetDefaultTextMapPropagator(new CompositeTextMapPropagator(new TextMapPropagator[]
// {
//     new TraceContextPropagator(),
//     new SentryPropagator()
//
//     // But don't include this.  It's already part of SentryPropagator.
//     // new BaggagePropagator()
// }));

builder.WebHost.UseSentry(options =>
{
    options.EnableTracing = true;
    options.Instrumenter = Instrumenter.OpenTelemetry;

    if (builder.Environment.IsDevelopment())
    {
        options.Debug = true;
    }
});

var app = builder.Build();

var httpClient = new HttpClient();
app.MapGet("/hello", async context =>
{
    var request = context.Request;
    var url = $"{request.Scheme}://{request.Host}{request.PathBase}/echo";
    var result = await httpClient.GetStringAsync(url);
    await context.Response.WriteAsync(result);
});

app.MapGet("/echo", () => "Hi!");

app.MapGet("/throw", _ => throw new Exception("test"));

app.Run();
