using Hangfire;

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
        configuration.UseFilter(new SentryJobFilter());
        return configuration;
    }
}
