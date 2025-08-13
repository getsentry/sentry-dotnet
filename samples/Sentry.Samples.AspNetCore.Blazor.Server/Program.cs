// Capture blazor bootstrapping errors

using AspNetCore.SignalR.OpenTelemetry;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Instrumentation.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sentry.OpenTelemetry;
using Sentry.Samples.AspNetCore.Blazor.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.TryAddSingleton<HubInstrumentationFilter>();
builder.Services.PostConfigure((Action<HubOptions>) (options => options.AddFilter<HubInstrumentationFilter>()));
builder.Services.Configure((Action<AspNetCoreTraceInstrumentationOptions>) (options => options.EnableAspNetCoreSignalRSupport = false));

// OpenTelemetry Configuration
// See https://opentelemetry.io/docs/instrumentation/net/getting-started/
builder.Services.AddOpenTelemetry()
    // This block configures OpenTelemetry to send traces to Sentry
    .WithTracing(tracerProviderBuilder =>
        tracerProviderBuilder
            // Here we add a custom source we've created, which sends telemetry in the `LookupUser` method below
            .AddSampleInstrumentation()
            // The two lines below take care of configuring sources for ASP.NET Core and HttpClient
            .AddAspNetCoreInstrumentation()
            .AddSignalRInstrumentation()
            .AddSentry() // <-- Configure OpenTelemetry to send traces to Sentry
    );
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.WebHost.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, in the SENTRY_DSN environment variable or in your appsettings.json
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif
    options.TracesSampleRate = 1.0;
    options.Debug = true;
    options.UseOpenTelemetry(); // <-- Configure Sentry to use OpenTelemetry trace information
});

var app = builder.Build();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
