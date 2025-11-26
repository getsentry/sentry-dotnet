// Capture blazor bootstrapping errors

using Microsoft.AspNetCore.Components.Server.Circuits;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.Samples.AspNetCore.Blazor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

#if NET10_0_OR_GREATER
// OpenTelemetry is required for the new .NET 10 Blazor telemetry features
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Microsoft.AspNetCore.Components");
        tracing.AddSource("Microsoft.AspNetCore.Components.Server.Circuits");
        tracing.AddAspNetCoreInstrumentation();
        // Add Sentry as an exporter
        tracing.AddSentry();
    })
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("Microsoft.AspNetCore.Components");
        metrics.AddMeter("Microsoft.AspNetCore.Components.Lifecycle");
        metrics.AddMeter("Microsoft.AspNetCore.Components.Server.Circuits");
        metrics.AddAspNetCoreInstrumentation();
    });
#endif

builder.WebHost.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, in the SENTRY_DSN environment variable or in your appsettings.json
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif
#if NET10_0_OR_GREATER
    options.UseOpenTelemetry();
    options.AddEventProcessor(new BlazorEventProcessor());
#endif
    options.TracesSampleRate = 1.0;
    options.Debug = true;
});

#if NET10_0_OR_GREATER
// Services to integrate with Blazor lifecycle events
builder.Services.AddSingleton<BlazorSentryIntegration>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<BlazorSentryIntegration>());
builder.Services.AddScoped<CircuitHandler, SentryCircuitHandler>();
#endif

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
