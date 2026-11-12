using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sentry.Samples.GenericHost;

var builder = Host.CreateApplicationBuilder();

// Initializes Sentry when the host starts and closes it when the host shuts down. Options are read from the
// "Sentry" section of appsettings.json first; anything set in this callback runs afterwards and wins.
// The host environment (DOTNET_ENVIRONMENT) becomes the Sentry environment unless one is configured.
builder.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set it here in code, in the "Sentry" section of appsettings.json,
    // or in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif
});

builder.Services.AddHostedService<SampleHostedService>();

await builder.Build().RunAsync();
