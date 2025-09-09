using Microsoft.Extensions.Logging;
using Sentry;
using Sentry.Extensibility;
using Sentry.Extensions.Logging;
using Sentry.Profiling;

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
            // If profiling enabled, disable it.
            RemoveBlazorProfilingIntegration(blazorOptions);
        });

        return builder;
    }

    private static void RemoveBlazorProfilingIntegration(SentryBlazorOptions options)
    {
        if (!options.IsProfilingEnabled)
        {
            return;
        }

        options.SetupLogging();
        options.LogDebug("Detected Sentry profiling initialization in Blazor WebAssembly." +
                         "Sentry does not support Blazor WebAssembly profiling. Removing profiling integration." +
                         "Check https://github.com/getsentry/sentry-dotnet/issues/4506 for more information.");
        // Ensure project doesn't have Profiling Integration
        options.RemoveIntegration<ProfilingIntegration>();
    }
}

/// <summary>
/// Sentry Blazor Options
/// </summary>
public class SentryBlazorOptions : SentryLoggingOptions
{
    // Awesome Blazor specific options go here
}
