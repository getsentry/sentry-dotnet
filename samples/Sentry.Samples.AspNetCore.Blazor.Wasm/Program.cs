using System.Diagnostics;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Samples.AspNetCore.Blazor.Wasm;

// Capture blazor bootstrapping errors
using var sdk = SentrySdk.Init(o =>
{
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    o.Debug = true;
    //IsGlobalModeEnabled will be true for Blazor WASM
    Debug.Assert(o.IsGlobalModeEnabled);
});
try
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);
    builder.RootComponents.Add<App>("#app");
    builder.Logging.SetMinimumLevel(LogLevel.Debug);
    // Captures logError and higher as events
    builder.Logging.AddSentry(o => o.InitializeSdk = false);

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
