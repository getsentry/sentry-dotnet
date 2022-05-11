using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using Sentry.AspNetCore;
using Sentry.Extensions.Logging.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Extension methods to <see cref="IHostBuilder"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class SentryHostBuilderExtensions
{
    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">An action that will configure Sentry.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static IHostBuilder UseSentry(
        this IHostBuilder builder,
        Action<SentryAspNetCoreOptions>? configureOptions)
    {
        // Signal to the chained UseSentry implementation that we will add the startup filter directly.
        builder.Properties[SentryAspNetCorePipelineHook.WillRegisterSentryAspNetCoreStartupFilter] = true;

        return builder
            .UseSentry<SentryAspNetCoreOptions>(configureOptions)
            .ConfigureServices(services => services.AddSentryStartupFilter());
    }

    /// <summary>
    /// Uses Sentry integration.
    /// </summary>
    /// <param name="builder">The builder.</param>
    /// <param name="configureOptions">An action that will configure Sentry.</param>
    /// <returns>The <paramref name="builder"/>.</returns>
    public static IHostBuilder UseSentry(
        this IHostBuilder builder,
        Action<HostBuilderContext, SentryAspNetCoreOptions>? configureOptions)
    {
        // Signal to the chained UseSentry implementation that we will add the startup filter directly.
        builder.Properties[SentryAspNetCorePipelineHook.WillRegisterSentryAspNetCoreStartupFilter] = true;

        return builder
            .UseSentry<SentryAspNetCoreOptions>(configureOptions)
            .ConfigureServices(services => services.AddSentryStartupFilter());
    }
}
