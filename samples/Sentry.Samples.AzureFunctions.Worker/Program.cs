using Microsoft.Extensions.Hosting;
using Sentry.AzureFunctions.Worker;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseSentry(options =>
        {
            options.EnableTracing = true;
            // options.Debug = true;
        });
    })
    .Build();

host.Run();
