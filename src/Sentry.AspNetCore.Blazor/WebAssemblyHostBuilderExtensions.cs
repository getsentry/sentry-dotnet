using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder UseSentry(this WebAssemblyHostBuilder builder, Action<SentryLoggingOptions> configureOptions)
    {
        builder.Logging.AddSentry(options =>
        {
            configureOptions(options);

            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            options.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            options.RequestBodyCompressionLevel = CompressionLevel.NoCompression;

        });
        return builder;
    }
}
