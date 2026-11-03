namespace Sentry.Integrations;

internal class GlobalRootScopeIntegration : ISdkIntegration
{
    public void Register(IHub hub, SentryOptions options)
    {
        if (!options.IsGlobalModeEnabled)
        {
            return;
        }

        hub.ConfigureScope(static (scope, options) => scope.User.Id ??= options.InstallationId, options);
    }
}
