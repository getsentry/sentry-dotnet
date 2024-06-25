using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Samples.AspNetCore.Blazor.Wasm;

try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.UseSentry(options =>
    {
        options.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
        options.Debug = true;
    });

    builder.RootComponents.Add<App>("#app");
    builder.Logging.SetMinimumLevel(LogLevel.Debug);

    builder.Services.AddScoped(_ =>
        new HttpClient
        {
            BaseAddress = new(builder.HostEnvironment.BaseAddress)
        });
    await builder.Build().RunAsync();
}
catch (Exception e)
{
    SentrySdk.CaptureException(e);
    await SentrySdk.FlushAsync();
    throw;
}
