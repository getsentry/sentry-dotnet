namespace Sentry;

internal static class DefaultIntegrationsExtensions
{
    public static bool Includes(this SentryOptions.DefaultIntegrations integrations, SentryOptions.DefaultIntegrations value)
        => (integrations & value) != 0;
}
