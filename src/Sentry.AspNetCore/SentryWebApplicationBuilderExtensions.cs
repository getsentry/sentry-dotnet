using Microsoft.AspNetCore.Hosting;
using Sentry.AspNetCore;

// ReSharper disable once CheckNamespace
namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Extension methods to <see cref="WebApplicationBuilder"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryWebApplicationBuilderExtensions
{
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    public static WebApplicationBuilder UseSentry(this WebApplicationBuilder builder)
    {
        builder.WebHost.UseSentry();
        return builder;
    }

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="dsn">The DSN.</param>
    public static WebApplicationBuilder UseSentry(this WebApplicationBuilder builder, string dsn)
    {
        builder.WebHost.UseSentry(dsn);
        return builder;
    }

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">The configure options.</param>
    public static WebApplicationBuilder UseSentry(
        this WebApplicationBuilder builder,
        Action<SentryAspNetCoreOptions>? configureOptions)
    {
        builder.WebHost.UseSentry(configureOptions);
        return builder;
    }

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureSentry">The Sentry builder.</param>
    public static WebApplicationBuilder UseSentry(
        this WebApplicationBuilder builder,
        Action<ISentryBuilder>? configureSentry)
    {
        builder.WebHost.UseSentry(configureSentry);
        return builder;
    }
}
