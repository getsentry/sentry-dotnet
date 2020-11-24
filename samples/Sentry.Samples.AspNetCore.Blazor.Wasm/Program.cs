using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Sentry.Samples.AspNetCore.Blazor.Wasm
{
    public static class Program
    {
#pragma warning disable IDE1006 // Naming Styles
        public static async Task Main(string[] args)
        {
            // Capture blazor bootstrapping errors
            using var sdk = SentrySdk.Init(o =>
            {
                o.Dsn = "https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537";
                o.Debug = true;
            });
            try
            {
                var builder = WebAssemblyHostBuilder.CreateDefault(args);
                builder.RootComponents.Add<App>("#app");
                builder.Logging.SetMinimumLevel(LogLevel.Debug);
                // Captures logError and higher as events
                builder.Logging.AddSentry(o => o.InitializeSdk = false);

                builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

                await builder.Build().RunAsync();
            }
            catch (Exception e)
            {
                SentrySdk.CaptureException(e);
                await SentrySdk.FlushAsync(TimeSpan.FromSeconds(2));
                throw;
            }
        }
    }
}
