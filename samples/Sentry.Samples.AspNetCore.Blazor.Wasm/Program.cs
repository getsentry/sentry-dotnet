using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Samples.AspNetCore.Blazor.Wasm;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.UseSentry(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // You must specify a DSN. On mobile platforms, this should be done in code here.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
#else
        // To make things easier for the SDK maintainers we have a custom build target that writes the
        // SENTRY_DSN environment variable into an EnvironmentVariables class that is available for WASM
        // targets. This allows us to share one DSN defined in the ENV across desktop and mobile samples.
        // Generally, you won't want to do this in your own WASM applications - you should set the DSN
        // in code as above
        options.Dsn = EnvironmentVariables.Dsn;
#endif
        options.Debug = true;
    });

    builder.RootComponents.Add<App>("#app");
    builder.RootComponents.Add<HeadOutlet>("head::after");
    builder.Logging.SetMinimumLevel(LogLevel.Debug);

    builder.Services.AddScoped(_ =>
        new HttpClient
        {
            BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
        });
    await builder.Build().RunAsync();
}
catch (Exception e)
{
    SentrySdk.CaptureException(e);
    await SentrySdk.FlushAsync();
    throw;
}
