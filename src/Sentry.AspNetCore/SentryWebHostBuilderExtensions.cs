using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
                SetEnvironment(context.HostingEnvironment, options);
            });
        });

#if NETSTANDARD2_0
    internal static void SetEnvironment(IHostingEnvironment hostingEnvironment, SentryAspNetCoreOptions options)
#else
    internal static void SetEnvironment(IWebHostEnvironment hostingEnvironment, SentryAspNetCoreOptions options)
#endif
    {
        // Set environment from AspNetCore hosting environment name, if not set already
        // Note: The SettingLocator will take care of the default behavior and assignment, which takes precedence.
        //       We only need to do anything here if nothing was found by the locator.
        if (options.SettingLocator.GetEnvironment(useDefaultIfNotFound: false) is not null)
        {
            return;
        }

        if (options.AdjustStandardEnvironmentNameCasing)
        {
            // NOTE: Sentry prefers to have its environment setting to be all lower case.
            //       .NET Core sets the ENV variable to 'Production' (upper case P),
            //       'Development' (upper case D) or 'Staging' (upper case S) which conflicts with
            //       the Sentry recommendation. As such, we'll be kind and override those values,
            //       here ... if applicable.
            // Assumption: The Hosting Environment is always set.
            //             If not set by a developer, then the framework will auto set it.
            //             Alternatively, developers might set this to a CUSTOM value, which we
            //             need to respect (especially the case-sensitivity).
            //             REF: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/environments

            if (hostingEnvironment.IsProduction())
            {
                options.Environment = Sentry.Internal.Constants.ProductionEnvironmentSetting;
            }
            else if (hostingEnvironment.IsStaging())
            {
                options.Environment = Sentry.Internal.Constants.StagingEnvironmentSetting;
            }
            else if (hostingEnvironment.IsDevelopment())
            {
                options.Environment = Sentry.Internal.Constants.DevelopmentEnvironmentSetting;
            }
            else
            {
                // Use the value set by the developer.
                options.Environment = hostingEnvironment.EnvironmentName;
            }
        }
        else
        {
            options.Environment = hostingEnvironment.EnvironmentName;
        }
    }

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
            _ = logging.Services.Configure<SentryAspNetCoreOptions>(section);

            _ = logging.Services
                .AddSingleton<IConfigureOptions<SentryAspNetCoreOptions>, SentryAspNetCoreOptionsSetup>();
            _ = logging.Services.AddSingleton<ILoggerProvider, SentryAspNetCoreLoggerProvider>();

            _ = logging.AddFilter<SentryAspNetCoreLoggerProvider>(
                "Microsoft.AspNetCore.Diagnostics.ExceptionHandlerMiddleware",
                LogLevel.None);

            var sentryBuilder = logging.Services.AddSentry();
            configureSentry?.Invoke(context, sentryBuilder);

        });

        _ = builder.ConfigureServices(c => _ = c.AddTransient<IStartupFilter, SentryStartupFilter>());

        return builder;
    }

    /// <summary>
    /// Adds and configures the Sentry tunneling middleware.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="hostnames">The extra hostnames to be allowed for the tunneling. sentry.io is allowed by default; add your own Sentry domain if you use a self-hosted Sentry or Relay.</param>
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
