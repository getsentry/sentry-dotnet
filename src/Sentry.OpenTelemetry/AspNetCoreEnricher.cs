namespace Sentry.OpenTelemetry;

internal class AspNetCoreEnricher : IOpenTelemetryEnricher
{
    private readonly ISentryUserFactory _userFactory;

    internal AspNetCoreEnricher(ISentryUserFactory userFactory) => _userFactory = userFactory;

    public void Enrich(ISpan span, Activity activity, IHub hub, SentryOptions? options)
    {
        if (options?.SendDefaultPii is true)
        {
            hub.ConfigureScope(scope =>
            {
                if (!scope.HasUser() && _userFactory.Create() is { } user)
                {
                    scope.User = user;
                }
            });
        }
    }
}
