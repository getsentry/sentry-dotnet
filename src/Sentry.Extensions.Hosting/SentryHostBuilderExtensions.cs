using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry;
using Sentry.Extensibility;
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
    private const string RedundantWithHostIntegration =
        "This UseSentry is redundant because the app also uses UseSentry from Sentry.AspNetCore or Sentry.Maui. Its " +
        "logger providers are disabled and its options ignored: call UseSentry from that package only.";

    private const string WebApplicationBuilder = "Microsoft.AspNetCore.Builder.WebApplicationBuilder";
    private const string WebApplicationHostBuilder = "Microsoft.AspNetCore.Builder.ConfigureHostBuilder";

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
        builder.Logging.AddSentryGenericHost(
            builder.Configuration,
            builder.Environment.EnvironmentName,
            configureOptions,
            IsWebApp(builder));
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
    {
        var isWebApp = IsWebApp(builder);
        return builder.ConfigureServices((context, services) => services.AddLogging(logging =>
            logging.AddSentryGenericHost(
                context.Configuration,
                context.HostingEnvironment.EnvironmentName,
                options => configureOptions?.Invoke(context, options),
                isWebApp)));
    }

    private static void AddSentryGenericHost(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string hostEnvironmentName,
        Action<SentryGenericHostOptions>? configureOptions,
        bool isWebApp)
    {
        var services = logging.Services;
        if (HasHostIntegration(services))
        {
            return;
        }

        if (!services.Any(IsGenericHostOptionsSetup))
        {
            logging.AddSentryGenericHostServices(configuration, hostEnvironmentName, isWebApp);
        }

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
    }

    private static void AddSentryGenericHostServices(
        this ILoggingBuilder logging,
        IConfiguration configuration,
        string hostEnvironmentName,
        bool isWebApp)
    {
        var services = logging.Services;
        logging.AddConfiguration();

        services.AddSingleton<IConfigureOptions<SentryGenericHostOptions>>(
            new SentryHostOptionsSetup<SentryGenericHostOptions>(configuration.GetSection("Sentry")));
        services.AddSingleton<IConfigureOptions<SentryGenericHostOptions>>(
            new SentryHostLoggingOptionsSetup<SentryGenericHostOptions>(configuration));

        services.PostConfigure<SentryGenericHostOptions>(options => options.SetEnvironment(hostEnvironmentName));

        services.AddSingleton<ILoggerProvider>(c =>
        {
            if (!HasHostIntegration(services))
            {
                return new SentryLoggerProvider(
                    c.GetRequiredService<IHub>(),
                    SystemClock.Clock,
                    c.GetRequiredService<IOptions<SentryGenericHostOptions>>().Value.Logging);
            }

            c.GetRequiredService<IHub>().GetSentryOptions()?.LogWarning(RedundantWithHostIntegration);
            return NullLoggerProvider.Instance;
        });
        services.AddSingleton<ILoggerProvider>(c => HasHostIntegration(services)
            ? NullLoggerProvider.Instance
            : new SentryStructuredLoggerProvider(c.GetRequiredService<IHub>()));
        services.TryAddSingleton<HostLoggerProvidersMarker>();
        services.AddSentry<SentryGenericHostOptions>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SentryHostedService>(c =>
            new SentryHostedService(c, isWebApp && !HasHostIntegration(services))));

        logging.AddFilter<SentryLoggerProvider>(_ => true);
        logging.AddFilter<SentryStructuredLoggerProvider>("Sentry.ISentryClient", LogLevel.None);
        logging.AddFilter<SentryStructuredLoggerProvider>("Sentry.Extensions.Hosting", LogLevel.None);
    }

    private static bool IsWebApp(object builder)
        => builder.GetType().FullName is WebApplicationBuilder or WebApplicationHostBuilder;

    private static bool HasHostIntegration(IServiceCollection services)
        => services.Any(d => d.ServiceType == typeof(HostIntegrationMarker));

    private static bool IsGenericHostOptionsSetup(ServiceDescriptor descriptor)
        => descriptor.ServiceType == typeof(IConfigureOptions<SentryGenericHostOptions>)
            && descriptor.ImplementationInstance is SentryHostOptionsSetup<SentryGenericHostOptions>;
}
