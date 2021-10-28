namespace Sentry.AspNetCore
{
    internal interface ISentryRouteName
    {
        public string? GetRouteName();
    }
}
