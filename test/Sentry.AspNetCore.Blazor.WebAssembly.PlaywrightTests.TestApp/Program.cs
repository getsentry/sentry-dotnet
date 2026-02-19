using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Sentry.AspNetCore.Blazor.WebAssembly.PlaywrightTests.TestApp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.UseSentry(options =>
{
    // Fake DSN — Playwright intercepts requests before they reach the network
    options.Dsn = "https://key@o0.ingest.sentry.io/0";
    options.AutoSessionTracking = false;
});

builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

await builder.Build().RunAsync();
