using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Samples.GenericHost;

var builder = Host.CreateApplicationBuilder();

builder.Logging.AddConfiguration(builder.Configuration);
builder.Logging.AddSentry();

builder.Services.AddHostedService<SampleHostedService>();

await builder.Build().RunAsync();
