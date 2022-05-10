using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
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
            sentryBuilder.AddSentryOptions(options => configureOptions?.Invoke(context, options)));

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
            var sentryBuilder = logging.AddSentry(context.Configuration);
            configureSentry?.Invoke(context, sentryBuilder);
        });

        _ = builder.ConfigureServices(c =>
        {
            _ = c.AddSentryStartupFilter();
        });

        return builder;
    }
}
