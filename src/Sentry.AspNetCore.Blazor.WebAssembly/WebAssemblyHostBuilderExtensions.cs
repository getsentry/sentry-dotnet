using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.AspNetCore.Blazor.WebAssembly.Internal;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Infrastructure;
using Sentry.Internal;
using Sentry.Reflection;

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
        builder.Logging.AddSentryBlazor(builder.Configuration, configureOptions);
        return builder;
    }

    internal static ILoggingBuilder AddSentryBlazor(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        Action<SentryBlazorOptions> configureOptions)
    {
        logging.AddConfiguration();

        logging.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>>(
            new SentryHostOptionsSetup<SentryBlazorOptions>(configuration.GetSection("Sentry")));
        logging.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>>(
            new SentryHostLoggingOptionsSetup<SentryBlazorOptions>(configuration));

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

        logging.Services.AddSingleton<IConfigureOptions<SentryBlazorOptions>, BlazorWasmOptionsSetup>();

        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryLoggerProvider(
            c.GetRequiredService<IHub>(),
            SystemClock.Clock,
            c.GetRequiredService<IOptions<SentryBlazorOptions>>().Value.Logging));
        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryStructuredLoggerProvider(c.GetRequiredService<IHub>()));
        logging.Services.TryAddSingleton<HostLoggerProvidersMarker>();
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
    /// <summary>
    /// Creates a new instance of <see cref="SentryBlazorOptions"/>.
    /// </summary>
    public SentryBlazorOptions()
    {
        SetSdk(Sentry.AspNetCore.Blazor.WebAssembly.Constants.SdkName, typeof(SentryBlazorOptions).Assembly.GetNameAndVersion());
    }

    /// <summary>
    /// Whether the <see cref="ILogger"/> integration that <c>UseSentry</c> adds sends log entries to Sentry as structured logs.
    /// </summary>
    /// <remarks>
    /// Defaults to <see langword="false"/>, including when the app also calls <c>AddSentry()</c> on its
    /// <see cref="ILoggingBuilder"/>.
    /// </remarks>
    public bool EnableLogs
    {
        get => LogsEnabled;
        set => LogsEnabled = value;
    }
}
