using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore.Blazor.WebAssembly.Internal;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

/// <summary>
/// Extension methods for <see cref="WebAssemblyHostBuilder"/>
/// </summary>
public static class WebAssemblyHostBuilderExtensions
{
    /// <summary>
    /// Use Sentry Integration
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="configureOptions"></param>
    /// <returns></returns>
    public static WebAssemblyHostBuilder UseSentry(this WebAssemblyHostBuilder builder, Action<SentryBlazorOptions> configureOptions)
    {
        builder.Logging.AddSentry<SentryBlazorOptions>(blazorOptions =>
        {
            configureOptions(blazorOptions);

            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            blazorOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            blazorOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
            // Since the WebAssemblyHost is a client-side application
            blazorOptions.IsGlobalModeEnabled = true;
        });

        builder.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>, BlazorWasmOptionsSetup>();

        return builder;
    }
}

/// <summary>
/// Sentry Blazor Options
/// </summary>
public class SentryBlazorOptions : SentryLoggingOptions
{
    // Awesome Blazor specific options go here
}
