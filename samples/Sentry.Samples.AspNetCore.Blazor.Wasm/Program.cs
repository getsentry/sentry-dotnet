using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.Samples.AspNetCore.Blazor.Wasm;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.UseSentry(o =>
{
    o.Dsn = "https://eb18e953812b41c3aeb042e666fd3b5c@o447951.ingest.sentry.io/5428537";
    o.Debug = true;
    o.TracesSampleRate = 1.0;

});
builder.RootComponents.Add<App>("#app");
builder.Logging.SetMinimumLevel(LogLevel.Debug);

builder.Services.AddScoped(_ =>
    new HttpClient
    {
        BaseAddress = new(builder.HostEnvironment.BaseAddress)
    });
await builder.Build().RunAsync();
