using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry.Samples.GenericHost;

await Host.CreateDefaultBuilder()
    .ConfigureServices(services =>
    {
        services.AddHostedService<SampleHostedService>();
    })
    .UseSentry()
    .UseSentry("https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537")
    .UseSentry(o =>
    {
        o.Debug = true;
        o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    })
    .UseSentry(o =>
    {
        o.Debug = true;
        o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
        o.MinimumEventLevel = LogLevel.Debug;
    })
    .UseSentry((c, o) =>
    {
        o.Debug = true;
        o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
        o.MinimumEventLevel = LogLevel.Debug;
    })
    .Build()
    .RunAsync();
