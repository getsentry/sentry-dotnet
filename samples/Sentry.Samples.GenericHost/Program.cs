using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Samples.GenericHost;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConfiguration(builder.Configuration);

#if !SENTRY_DSN_DEFINED_IN_ENV
// A DSN is required. You can set it here in code, via the SENTRY_DSN environment variable or in your
// appsettings.json file.
// See https://docs.sentry.io/platforms/dotnet/guides/aspnetcore/#configure
builder.Logging.AddSentry(SamplesShared.Dsn);
#else
builder.Logging.AddSentry();
#endif

builder.Services.AddHostedService<SampleHostedService>();

await builder.Build().RunAsync();
