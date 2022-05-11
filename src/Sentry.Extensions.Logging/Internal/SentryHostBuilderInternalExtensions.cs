using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Sentry.Extensions.Logging.Internal;

internal static class SentryHostBuilderInternalExtensions
{
    public static IHostBuilder UseSentry<TOptions>(this IHostBuilder builder)
        where TOptions : SentryLoggingOptions =>
        builder.UseSentry((Action<TOptions>?)null);

    public static IHostBuilder UseSentry<TOptions>(this IHostBuilder builder, string dsn)
        where TOptions : SentryLoggingOptions =>
        builder.UseSentry<TOptions>(o => o.Dsn = dsn);

    public static IHostBuilder UseSentry<TOptions>(this IHostBuilder builder, Action<TOptions>? configureOptions)
        where TOptions : SentryLoggingOptions =>
        builder.UseSentry<TOptions>((_, options) => configureOptions?.Invoke(options));

    public static IHostBuilder UseSentry<TOptions>(this IHostBuilder builder,
        Action<HostBuilderContext, TOptions>? configureOptions)
        where TOptions : SentryLoggingOptions =>
        builder.UseSentry((context, sentryBuilder) =>
            sentryBuilder.AddSentryOptions<TOptions>(options =>
                configureOptions?.Invoke(context, options)));

    public static IHostBuilder UseSentry(this IHostBuilder builder,
        Action<ISentryBuilder>? configureSentry) =>
        builder.UseSentry((_, sentryBuilder) => configureSentry?.Invoke(sentryBuilder));

    public static IHostBuilder UseSentry(this IHostBuilder builder,
        Action<HostBuilderContext, ISentryBuilder>? configureSentry)
    {
        builder.ConfigureLogging((context, logging) =>
        {
            var sentryBuilder = logging.AddSentry(context.Configuration);
            configureSentry?.Invoke(context, sentryBuilder);
        });

        var registeredStartupFilter = builder.TryRegisterAspNetCoreStartupFilterIfNeeded();

        if (!registeredStartupFilter)
        {
            // We're not in ASP.NET Core, so do the rest of the startup configuration
            // in a hosted service instead of a startup filter.
            builder.ConfigureServices(services => services.AddHostedService<SentryStartupService>());
        }

        return builder;
    }

}
