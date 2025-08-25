using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;
using Sentry.Azure.Functions.Worker;



var builder = FunctionsApplication.CreateBuilder(args);
builder.UseSentry(options =>
{
#if !SENTRY_DSN_DEFINED_IN_ENV
            // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
            options.Dsn = SamplesShared.Dsn;
#endif
    options.TracesSampleRate = 1.0;
    options.Debug = true;
});
builder.ConfigureFunctionsWebApplication();

var host = builder.Build();

await host.RunAsync();
