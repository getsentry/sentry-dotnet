using Microsoft.Extensions.Hosting;
using Sentry.Azure.Functions.Worker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults((host, builder) =>
    {
        builder.UseSentry(host, options =>
        {
            options.EnableTracing = true;
            // options.Debug = true;
        });
    })
    .Build();

await host.RunAsync();
