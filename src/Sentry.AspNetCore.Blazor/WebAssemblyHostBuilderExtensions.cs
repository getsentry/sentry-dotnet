using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensions.Logging;

// ReSharper disable once CheckNamespace - Discoverability
namespace Microsoft.AspNetCore.Components.WebAssembly.Hosting;

public static class WebAssemblyHostBuilderExtensions
{
    public static WebAssemblyHostBuilder UseSentry(this WebAssemblyHostBuilder builder, Action<SentryBlazorOptions> configureOptions)
    {
        var blazorOptions = new SentryBlazorOptions();

        if (blazorOptions.ReplaysSessionSampleRate > 0)
        {
            // Add Session Replay
        }
        builder.Logging.AddSentry<SentryBlazorOptions>(loggingOptions =>
        {
            configureOptions(blazorOptions);

            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            loggingOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            loggingOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;

        });
        return builder;
    }
}

public class SentryBlazorOptions : SentryLoggingOptions
{
    // https://docs.sentry.io/platforms/javascript/session-replay/
    public int ReplaysSessionSampleRate { get; set; }
    public int ReplaysOnErrorSampleRate { get; set; }
}
