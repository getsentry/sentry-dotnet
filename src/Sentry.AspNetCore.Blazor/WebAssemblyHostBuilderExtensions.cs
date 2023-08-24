using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.AspNetCore.Blazor;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder UseSentry(this WebAssemblyHostBuilder builder, Action<SentryBlazorOptions> configureOptions)
    {
        builder.Services.TryAddSingleton<IScopeObserver, JavaScriptScopeObserver>();
        builder.Logging.AddSentry<SentryBlazorOptions>(blazorOptions =>
        {
            configureOptions(blazorOptions);

            blazorOptions.EnableScopeSync = true;
            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            blazorOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            blazorOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
            blazorOptions.UseStackTraceFactory(
                new WasmStackTraceFactory(
                    new SentryStackTraceFactory(blazorOptions),
                    blazorOptions));
        });
        return builder;
    }
}

public class SentryBlazorOptions : SentryLoggingOptions
{
    internal Func<DebugImage[]>? GetDebugImages { get; set; }
}
