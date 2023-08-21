using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public static class WebAssemblyHostBuilderExtensions
{
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

public class SentryBlazorOptions : SentryLoggingOptions
{
    // https://docs.sentry.io/platforms/javascript/session-replay/
    public double ReplaysOnErrorSampleRate { get; set; }
    public double ReplaysSessionSampleRate { get; set; }
    public bool MaskAllText { get; set; } = true;
    public bool BlockAllMedia { get; set; } = true;
}
