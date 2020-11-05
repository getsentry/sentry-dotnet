using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Sentry.Samples.AspNetCore.Blazor.Wasm
{
    public static class Program
    {
#pragma warning disable IDE1006 // Naming Styles
        public static async Task Main(string[] args)
        {
            using var sdk = SentrySdk.Init("https://80aed643f81249d4bed3e30687b310ab@o447951.ingest.sentry.io/5428537");

            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("app");

            _ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            await builder.Build().RunAsync();
        }
    }
}
