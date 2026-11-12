namespace Sentry.Internal;

internal class Enricher
{
    internal const string DefaultIpAddress = "{{auto}}";

    private readonly SentryOptions _options;

    public Enricher(SentryOptions options) => _options = options;

    public void Apply(IEventLike eventLike)
    {
        _options.ScopeDefaults.Apply(eventLike);

        // User
        // Report local user if opt-in PII, no user was already set to event and feature not opted-out:
        if (_options.SendDefaultPii)
        {
            if (_options.IsEnvironmentUser && !eventLike.HasUser())
            {
                eventLike.User.Username = Environment.UserName;
            }

            eventLike.User.IpAddress ??= DefaultIpAddress;
        }
        // Set by the GlobalRootScopeIntegration in global mode so that it can be overridden by the user.
        // In non-global mode (e.g. ASP.NET Core) the enricher sets it here as a fallback.
        if (!_options.IsGlobalModeEnabled)
        {
            eventLike.User.Id ??= _options.InstallationId;
        }

        eventLike.Contexts.App.StartTime ??= ProcessInfo.Instance?.StartupTime;
        eventLike.Contexts.App.InForeground = ProcessInfo.Instance?.ApplicationIsActivated(_options);
    }

    public void Apply(SentryCheckIn checkIn, Scope? scope) => _options.ScopeDefaults.Apply(checkIn, scope);
}
