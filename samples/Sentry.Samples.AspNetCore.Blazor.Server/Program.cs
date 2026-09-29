// Capture blazor bootstrapping errors

using Microsoft.AspNetCore.Components.Server.Circuits;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry.Exporter;
using Sentry.Samples.AspNetCore.Blazor.Server.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

#if SENTRY_DSN_DEFINED_IN_ENV
var dsn = Environment.GetEnvironmentVariable("SENTRY_DSN")
          ?? throw new InvalidOperationException("SENTRY_DSN environment variable is not set");
#else
// A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
// See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
var dsn = SamplesShared.Dsn;
#endif

#if NET10_0_OR_GREATER
// OpenTelemetry is required for the new .NET 10 Blazor telemetry features
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddSource("Microsoft.AspNetCore.Components");
        tracing.AddSource("Microsoft.AspNetCore.Components.Server.Circuits");
        tracing.AddAspNetCoreInstrumentation();
        // Add Sentry as an exporter
        tracing.AddSentryOtlpExporter(dsn);
    });
#endif

builder.WebHost.UseSentry(options =>
{
    options.Dsn = dsn;
#if NET10_0_OR_GREATER
    options.UseOtlp();
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
