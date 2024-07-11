using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Hosting;

/// <summary>
/// Extension methods to <see cref="IWebHostBuilder"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryWebHostBuilderExtensions
{
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static IWebHostBuilder UseSentry(this IWebHostBuilder builder)
        => UseSentry(builder, (Action<SentryAspNetCoreOptions>?)null);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    public static IWebHostBuilder UseSentry(this IWebHostBuilder builder, string dsn)
        => builder.UseSentry(o => o.Dsn = dsn);

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static IWebHostBuilder UseSentry(
        this IWebHostBuilder builder,
        Action<SentryAspNetCoreOptions>? configureOptions)
        => builder.UseSentry((_, options) => configureOptions?.Invoke(options));

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static IWebHostBuilder UseSentry(
        this IWebHostBuilder builder,
        Action<WebHostBuilderContext, SentryAspNetCoreOptions>? configureOptions)
        => builder.UseSentry((context, sentryBuilder) =>
        {
            sentryBuilder.AddSentryOptions(options =>
            {
                configureOptions?.Invoke(context, options);
                options.SetEnvironment(context.HostingEnvironment);
            });
        });

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureSentry">The Sentry builder.</param>
    public static IWebHostBuilder UseSentry(
        this IWebHostBuilder builder,
        Action<ISentryBuilder>? configureSentry) =>
        builder.UseSentry((_, sentryBuilder) => configureSentry?.Invoke(sentryBuilder));

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureSentry">The Sentry builder.</param>
    public static IWebHostBuilder UseSentry(
        this IWebHostBuilder builder,
        Action<WebHostBuilderContext, ISentryBuilder>? configureSentry)
    {
        // The earliest we can hook the SDK initialization code with the framework
        // Initialization happens at a later time depending if the default MEL backend is enabled or not.
        // In case the logging backend was replaced, init happens later, at the StartupFilter
        _ = builder.ConfigureLogging((context, logging) =>
        {
            logging.AddConfiguration();

            var section = context.Configuration.GetSection("Sentry");
#if NETSTANDARD2_0
            _ = logging.Services.Configure<SentryAspNetCoreOptions>(section);
#else
            _ = logging.Services.AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>>(_ =>
                new SentryAspNetCoreOptionsSetup(section)
            );
#endif
            _ = logging.Services
                .AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            _ = logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            _ = logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

            var sentryBuilder = logging.Services.AddSentry();
            configureSentry?.Invoke(context, sentryBuilder);
        });

        _ = builder.ConfigureServices(c => _ =
            c.AddSingleton(new LifetimeServiceResolver(c))
             .AddTransient<IStartupFilter, SentryStartupFilter>()
             .AddTransient<IStartupFilter, SentryTracingStartupFilter>()
             .AddTransient<SentryMiddleware>()
        );

        return builder;
    }

    /// <summary>
    /// Adds and configures the Sentry tunneling middleware.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="hostnames">
    /// The extra hostnames to be allowed for the tunneling.
    /// Hosts ending in <c>.sentry.io</c> are always allowed, and do not need to be included in this list.
    /// Add your own domain if you use a self-hosted Sentry or Relay.
    /// </param>
    public static void AddSentryTunneling(this IServiceCollection services, params string[] hostnames) =>
        services.AddScoped(_ => new SentryTunnelMiddleware(hostnames));

    /// <summary>
    /// Adds the <see cref="SentryTunnelMiddleware"/> to the pipeline.
    /// </summary>
    /// <param name="builder">The app builder.</param>
    /// <param name="path">The path to listen for Sentry envelopes.</param>
    public static void UseSentryTunneling(this IApplicationBuilder builder, string path = "/tunnel") =>
        builder.Map(path, applicationBuilder =>
            applicationBuilder.UseMiddleware<SentryTunnelMiddleware>());
}
