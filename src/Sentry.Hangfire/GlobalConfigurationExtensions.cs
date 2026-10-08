using Hangfire;
using Sentry.Extensibility;

namespace Sentry.Hangfire;

/// <summary>
/// Hangfire Extensions for <see cref="GlobalConfigurationExtensions"/>.
/// </summary>
public static class GlobalConfigurationExtensions
{
    /// <summary>
    /// Uses Sentry
    /// </summary>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IGlobalConfiguration UseSentry(this IGlobalConfiguration configuration)
    {
        configuration.UseFilter(new SentryServerFilter());
        return configuration;
    }

    /// <summary>
    /// Adds the Sentry filter that captures check-ins for jobs marked with <see cref="SentryMonitorSlugAttribute"/>.
    /// </summary>
    /// <param name="configuration">The Hangfire configuration.</param>
    /// <param name="configureOptions">Configures the Sentry Hangfire integration.</param>
    /// <returns>The Hangfire configuration.</returns>
    public static IGlobalConfiguration UseSentry(this IGlobalConfiguration configuration, Action<SentryHangfireOptions> configureOptions)
    {
        if (configureOptions is null)
        {
            throw new ArgumentNullException(nameof(configureOptions));
        }

        var options = new SentryHangfireOptions();
        configureOptions(options);
        configuration.UseFilter(new SentryServerFilter(null, null, options));
        return configuration;
    }

    /// <summary>
    /// For testing
    /// </summary>
    /// <param name="configuration"></param>
    /// <param name="hub"></param>
    /// <param name="logger"></param>
    /// <returns></returns>
    internal static IGlobalConfiguration UseSentry(this IGlobalConfiguration configuration, IHub hub, IDiagnosticLogger logger)
    {
        configuration.UseFilter(new SentryServerFilter(hub, logger));
        return configuration;
    }
}
