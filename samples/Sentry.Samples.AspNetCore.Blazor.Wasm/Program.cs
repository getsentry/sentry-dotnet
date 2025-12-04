using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Samples.AspNetCore.Blazor.Wasm;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.UseSentry(options =>
    {
#if !SENTRY_DSN_DEFINED_IN_ENV
        // A DSN is required. You can set here in code, or you can set it in the SENTRY_DSN environment variable.
        // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
        options.Dsn = SamplesShared.Dsn;
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
