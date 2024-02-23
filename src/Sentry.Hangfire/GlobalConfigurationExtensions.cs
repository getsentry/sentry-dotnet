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
