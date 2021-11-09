namespace Sentry.AspNetCore;

internal interface ISentryRouteName
{
    string? GetRouteName();
}
