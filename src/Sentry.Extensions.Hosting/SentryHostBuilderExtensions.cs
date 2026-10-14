using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.Extensions.Hosting;
using Sentry.Extensions.Logging;
using Sentry.Extensions.Logging.Extensions.DependencyInjection;
using Sentry.Infrastructure;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods to <see cref="IHostBuilder"/> and <c>IHostApplicationBuilder</c>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryHostBuilderExtensions
{
#if NET8_0_OR_GREATER
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static IHostApplicationBuilder UseSentry(this IHostApplicationBuilder builder)
        => builder.UseSentry((Action<SentryGenericHostOptions>?)null);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    public static IHostApplicationBuilder UseSentry(this IHostApplicationBuilder builder, string dsn)
        => builder.UseSentry(options => options.Dsn = dsn);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static IHostApplicationBuilder UseSentry(
        this IHostApplicationBuilder builder,
        Action<SentryGenericHostOptions>? configureOptions)
    {
        builder.Logging.AddSentryGenericHost(builder.Configuration, builder.Environment.EnvironmentName, configureOptions);
        return builder;
    }
#endif

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static IHostBuilder UseSentry(this IHostBuilder builder)
        => builder.UseSentry((Action<SentryGenericHostOptions>?)null);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    public static IHostBuilder UseSentry(this IHostBuilder builder, string dsn)
        => builder.UseSentry(options => options.Dsn = dsn);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static IHostBuilder UseSentry(this IHostBuilder builder, Action<SentryGenericHostOptions>? configureOptions)
        => builder.UseSentry((_, options) => configureOptions?.Invoke(options));

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static IHostBuilder UseSentry(
        this IHostBuilder builder,
        Action<HostBuilderContext, SentryGenericHostOptions>? configureOptions)
        => builder.ConfigureServices((context, services) => services.AddLogging(logging =>
            logging.AddSentryGenericHost(
                context.Configuration,
                context.HostingEnvironment.EnvironmentName,
                options => configureOptions?.Invoke(context, options))));

    private static ILoggingBuilder AddSentryGenericHost(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string hostEnvironmentName,
        Action<SentryGenericHostOptions>? configureOptions)
    {
        logging.AddConfiguration();

        logging.Services.AddSingleton<IConfigureOptions<SentryGenericHostOptions>>(
            new SentryHostOptionsSetup<SentryGenericHostOptions>(configuration.GetSection("Sentry")));
        logging.Services.AddSingleton<IConfigureOptions<SentryGenericHostOptions>>(
            new SentryHostLoggingOptionsSetup<SentryGenericHostOptions>(configuration));

        logging.Services.Configure<SentryGenericHostOptions>(options =>
        {
            configureOptions?.Invoke(options);
            options.SetEnvironment(hostEnvironmentName);
        });

        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryLoggerProvider(
            c.GetRequiredService<IHub>(),
            SystemClock.Clock,
            c.GetRequiredService<IOptions<SentryGenericHostOptions>>().Value.Logging));
        logging.Services.AddSingleton<ILoggerProvider>(c => new SentryStructuredLoggerProvider(c.GetRequiredService<IHub>()));
        logging.Services.TryAddSingleton<HostLoggerProvidersMarker>();
        logging.Services.AddSentry<SentryGenericHostOptions>();

        logging.AddFilter<SentryLoggerProvider>(_ => true);
        logging.AddFilter<SentryStructuredLoggerProvider>("Sentry.ISentryClient", LogLevel.None);

        return logging;
    }
}
