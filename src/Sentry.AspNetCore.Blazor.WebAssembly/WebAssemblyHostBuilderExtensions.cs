using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore.Blazor.WebAssembly.Internal;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Infrastructure;
using Sentry.Internal;

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
        builder.Logging.AddSentryBlazor(configureOptions);
        return builder;
    }

    internal static ILoggingBuilder AddSentryBlazor(this ILoggingBuilder logging, Action<SentryBlazorOptions> configureOptions)
    {
        logging.AddConfiguration();

        logging.Services.Configure<SentryBlazorOptions>(blazorOptions =>
        {
            configureOptions(blazorOptions);

            // System.PlatformNotSupportedException: System.Diagnostics.Process is not supported on this platform.
            blazorOptions.DetectStartupTime = StartupTimeDetectionMode.Fast;
            // Warning: No response compression supported by HttpClientHandler.
            blazorOptions.RequestBodyCompressionLevel = CompressionLevel.NoCompression;
            // Since the WebAssemblyHost is a client-side application
            blazorOptions.IsGlobalModeEnabled = true;
            blazorOptions.AddTransactionProcessor(new TraceIgnoreStatusCodeTransactionProcessor(blazorOptions));
        });

        logging.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>, SentryHostOptionsSetup<SentryBlazorOptions>>();
        logging.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>, BlazorWasmOptionsSetup>();

        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryLoggerProvider(
            c.GetRequiredService<IHub>(),
            SystemClock.Clock,
            c.GetRequiredService<IOptions<SentryBlazorOptions>>().Value.Logging));
        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryStructuredLoggerProvider(c.GetRequiredService<IHub>()));
        logging.Services.AddSentry<SentryBlazorOptions>();

        logging.AddFilter<SentryLoggerProvider>(_ => true);
        logging.AddFilter<SentryStructuredLoggerProvider>("Sentry.ISentryClient", LogLevel.None);

        return logging;
    }
}

/// <summary>
/// Sentry Blazor Options
/// </summary>
public class SentryBlazorOptions : SentryHostOptions
{
    // Awesome Blazor specific options go here
}
