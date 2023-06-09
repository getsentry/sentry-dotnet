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
            .AddProcessor<SentrySpanProcessor>()
            .AddConsoleExporter());

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

// TODO: When instrumenting with OpenTelemetry, what do we still need from the SentryTracing middleware?
// app.UseSentryTracing();

// TODO: When instrumenting with OpenTelemetry, do we need to use the SentryHttpMessageHandler?
var httpClient = new HttpClient();
app.MapGet("/hello", async () => await httpClient.GetStringAsync("https://example.com/"));

app.MapGet("/throw", _ => throw new Exception("test"));

app.Run();
