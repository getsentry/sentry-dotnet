using Microsoft.Extensions.Logging;
using Sentry;
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
        });

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
