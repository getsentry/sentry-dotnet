using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Samples.GenericHost;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConfiguration(builder.Configuration);

// Initialise the Sentry SDK. The logging integration added below only forwards log messages to Sentry.
using var sentry = SentrySdk.Init(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
    // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
    // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
    options.Dsn = SamplesShared.Dsn;
#endif
    // Send user name and machine name
    options.SendDefaultPii = true;
});

builder.Logging.AddSentry();

builder.Services.AddHostedService<SampleHostedService>();

await builder.Build().RunAsync();
